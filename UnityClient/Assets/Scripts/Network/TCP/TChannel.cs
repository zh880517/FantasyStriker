using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
//using Microsoft.IO;

namespace Net
{
	/// <summary>
	/// 封装Socket,将回调push到主线程处理
	/// </summary>
	public sealed class TChannel: AChannel
	{
		private Socket socket;
		private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
		private SocketAsyncEventArgs outArgs = new SocketAsyncEventArgs();

		private readonly CircularBuffer recvBuffer = new CircularBuffer();
		private readonly CircularBuffer sendBuffer = new CircularBuffer();

		private readonly MemoryStream memoryStream;

		private bool isSending;

		private bool isConnected;

		private readonly PacketParser parser;

		private readonly byte[] packetSizeCache;

		private readonly IPEndPoint remoteIpEndPoint;
		
		public TChannel(IPEndPoint ipEndPoint, TService service): base(service, ChannelType.Connect)
		{
			int packetSize = service.PacketSizeLength;
			packetSizeCache = new byte[packetSize];
			memoryStream = service.MemoryStreamManager.GetStream("message", ushort.MaxValue);
			
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.NoDelay = true;
			parser = new PacketParser(packetSize, recvBuffer, memoryStream);
			innArgs.Completed += OnComplete;
			outArgs.Completed += OnComplete;

			RemoteAddress = ipEndPoint.ToString();
			remoteIpEndPoint = ipEndPoint;
			isConnected = false;
			isSending = false;
		}
		
		public TChannel(Socket socket, TService service): base(service, ChannelType.Accept)
		{
			int packetSize = service.PacketSizeLength;
			packetSizeCache = new byte[packetSize];
			memoryStream = service.MemoryStreamManager.GetStream("message", ushort.MaxValue);
			
			this.socket = socket;
			socket.NoDelay = true;
			parser = new PacketParser(packetSize, recvBuffer, memoryStream);
			innArgs.Completed += OnComplete;
			outArgs.Completed += OnComplete;

			RemoteAddress = socket.RemoteEndPoint.ToString();
			remoteIpEndPoint = (IPEndPoint)socket.RemoteEndPoint;
			isConnected = true;
			isSending = false;
		}

		public override void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}
			
			base.Dispose();
			
			socket.Close();
			innArgs.Dispose();
			outArgs.Dispose();
			innArgs = null;
			outArgs = null;
			socket = null;
			memoryStream.Dispose();
		}

		public override void Start()
		{
			if (ChannelType == ChannelType.Accept)
			{
				StartRecv();
			}
			else
			{
				ConnectAsync(remoteIpEndPoint);
			}
		}
		
		private TService GetService()
		{
			return (TService)Service;
		}

		public override MemoryStream Stream
		{
			get
			{
				return memoryStream;
			}
		}
		
		public override void Send(MemoryStream stream)
		{
			if (IsDisposed)
			{
				throw new Exception("TChannel已经被Dispose, 不能发送消息");
			}

			switch (GetService().PacketSizeLength)
			{
				case Packet.PacketSizeLength4:
					if (stream.Length > ushort.MaxValue * 16)
					{
						throw new Exception($"send packet too large: {stream.Length}");
					}
					packetSizeCache.WriteTo(0, (int) stream.Length);
					break;
				case Packet.PacketSizeLength2:
					if (stream.Length > ushort.MaxValue)
					{
						throw new Exception($"send packet too large: {stream.Length}");
					}
					packetSizeCache.WriteTo(0, (ushort) stream.Length);
					break;
				default:
					throw new Exception("packet size must be 2 or 4!");
			}

			sendBuffer.Write(packetSizeCache, 0, packetSizeCache.Length);
			sendBuffer.Write(stream);

			GetService().MarkNeedStartSend(Id);
		}

		private void OnComplete(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Connect:
					OneThreadSynchronizationContext.Instance.Post(OnConnectComplete, e);
					break;
				case SocketAsyncOperation.Receive:
					OneThreadSynchronizationContext.Instance.Post(OnRecvComplete, e);
					break;
				case SocketAsyncOperation.Send:
					OneThreadSynchronizationContext.Instance.Post(OnSendComplete, e);
					break;
				case SocketAsyncOperation.Disconnect:
					OneThreadSynchronizationContext.Instance.Post(OnDisconnectComplete, e);
					break;
				default:
					throw new Exception($"socket error: {e.LastOperation}");
			}
		}

		public void ConnectAsync(IPEndPoint ipEndPoint)
		{
			outArgs.RemoteEndPoint = ipEndPoint;
			if (socket.ConnectAsync(outArgs))
			{
				return;
			}
			OnConnectComplete(outArgs);
		}

		private void OnConnectComplete(object o)
		{
			if (socket == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs) o;
			
			if (e.SocketError != SocketError.Success)
			{
				OnError((int)e.SocketError);	
				return;
			}

			e.RemoteEndPoint = null;
			isConnected = true;
			StartRecv();
			GetService().MarkNeedStartSend(Id);
		}

		private void OnDisconnectComplete(object o)
		{
			SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
			OnError((int)e.SocketError);
		}

		public void StartRecv()
		{
			int size = recvBuffer.ChunkSize - recvBuffer.LastIndex;
			RecvAsync(recvBuffer.Last, recvBuffer.LastIndex, size);
		}

		public void RecvAsync(byte[] buffer, int offset, int count)
		{
			try
			{
				innArgs.SetBuffer(buffer, offset, count);
			}
			catch (Exception e)
			{
				throw new Exception($"socket set buffer error: {buffer.Length}, {offset}, {count}", e);
			}
			
			if (socket.ReceiveAsync(innArgs))
			{
				return;
			}
			OnRecvComplete(innArgs);
		}

		private void OnRecvComplete(object o)
		{
			if (socket == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs) o;

			if (e.SocketError != SocketError.Success)
			{
				OnError((int)e.SocketError);
				return;
			}

			if (e.BytesTransferred == 0)
			{
				OnError(ErrorCode.ERR_PeerDisconnect);
				return;
			}

			recvBuffer.LastIndex += e.BytesTransferred;
			if (recvBuffer.LastIndex == recvBuffer.ChunkSize)
			{
				recvBuffer.AddLast();
				recvBuffer.LastIndex = 0;
			}

			// 收到消息回调
			while (true)
			{
				try
				{
					if (!parser.Parse())
					{
						break;
					}
				}
				catch (Exception ee)
				{
					Log.Error($"ip: {RemoteAddress} {ee}");
					OnError(ErrorCode.ERR_SocketError);
					return;
				}

				try
				{
					OnRead(parser.GetPacket());
				}
				catch (Exception ee)
				{
					Log.Error(ee);
				}
			}

			if (socket == null)
			{
				return;
			}
			
			StartRecv();
		}

		public bool IsSending => isSending;

		public void StartSend()
		{
			if(!isConnected)
			{
				return;
			}
			
			// 没有数据需要发送
			if (sendBuffer.Length == 0)
			{
				isSending = false;
				return;
			}

			isSending = true;

			int sendSize = sendBuffer.ChunkSize - sendBuffer.FirstIndex;
			if (sendSize > sendBuffer.Length)
			{
				sendSize = (int)sendBuffer.Length;
			}

			SendAsync(sendBuffer.First, sendBuffer.FirstIndex, sendSize);
		}

		public void SendAsync(byte[] buffer, int offset, int count)
		{
			try
			{
				outArgs.SetBuffer(buffer, offset, count);
			}
			catch (Exception e)
			{
				throw new Exception($"socket set buffer error: {buffer.Length}, {offset}, {count}", e);
			}
			if (socket.SendAsync(outArgs))
			{
				return;
			}
			OnSendComplete(outArgs);
		}

		private void OnSendComplete(object o)
		{
			if (socket == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs) o;

			if (e.SocketError != SocketError.Success)
			{
				OnError((int)e.SocketError);
				return;
			}
			
			if (e.BytesTransferred == 0)
			{
				OnError(ErrorCode.ERR_PeerDisconnect);
				return;
			}
			
			sendBuffer.FirstIndex += e.BytesTransferred;
			if (sendBuffer.FirstIndex == sendBuffer.ChunkSize)
			{
				sendBuffer.FirstIndex = 0;
				sendBuffer.RemoveFirst();
			}
			
			StartSend();
		}
	}
}