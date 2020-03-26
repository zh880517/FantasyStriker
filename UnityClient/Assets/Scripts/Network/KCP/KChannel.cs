using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Net
{
	public struct WaitSendBuffer
	{
		public byte[] Bytes;
		public int Length;

		public WaitSendBuffer(byte[] bytes, int length)
		{
			Bytes = bytes;
			Length = length;
		}
	}

	public class KChannel : AChannel
	{
		private Socket socket;

		private IntPtr kcp;

		private readonly Queue<WaitSendBuffer> sendBuffer = new Queue<WaitSendBuffer>();

		private bool isConnected;
		
		private readonly IPEndPoint remoteEndPoint;

		private uint lastRecvTime;
		
		private readonly uint createTime;

		public uint RemoteConn { get; private set; }

		private readonly MemoryStream memoryStream;

		// accept
		public KChannel(uint localConn, uint remoteConn, Socket socket, IPEndPoint remoteEndPoint, KService kService) : base(kService, ChannelType.Accept)
		{
			memoryStream = GetService().MemoryStreamManager.GetStream("message", ushort.MaxValue);

			LocalConn = localConn;
			RemoteConn = remoteConn;
			this.remoteEndPoint = remoteEndPoint;
			this.socket = socket;
			kcp = Kcp.KcpCreate(RemoteConn, new IntPtr(LocalConn));

			SetOutput();
			Kcp.KcpNodelay(kcp, 1, 10, 1, 1);
			Kcp.KcpWndsize(kcp, 256, 256);
			Kcp.KcpSetmtu(kcp, 470);
			lastRecvTime = kService.TimeNow;
			createTime = kService.TimeNow;
			Accept();
		}

		// connect
		public KChannel(uint localConn, Socket socket, IPEndPoint remoteEndPoint, KService kService) : base(kService, ChannelType.Connect)
		{
			memoryStream = GetService().MemoryStreamManager.GetStream("message", ushort.MaxValue);

			LocalConn = localConn;
            this.socket = socket;
            this.remoteEndPoint = remoteEndPoint;
			lastRecvTime = kService.TimeNow;
			createTime = kService.TimeNow;
			Connect();
		}

		public uint LocalConn
		{
			get
			{
				return (uint)Id;
			}
			set
			{
				Id = value;
			}
		}

		public override void Dispose()
		{
            if (IsDisposed)
                return;
			try
			{
				if (Error == ErrorCode.ERR_Success)
				{
					for (int i = 0; i < 4; i++)
					{
						Disconnect();
					}
				}
			}
			catch (Exception)
			{
				// ignored
			}

			if (kcp != IntPtr.Zero)
			{
				Kcp.KcpRelease(kcp);
				kcp = IntPtr.Zero;
			}
			socket = null;
			memoryStream.Dispose();
		}

		public override MemoryStream Stream
		{
			get
			{
				return memoryStream;
			}
		}

		public void Disconnect(int error)
		{
			OnError(error);
		}

		private KService GetService()
		{
			return (KService)Service;
		}

		public void HandleConnnect(uint remoteConn)
		{
			if (isConnected)
			{
				return;
			}

			RemoteConn = remoteConn;

			kcp = Kcp.KcpCreate(RemoteConn, new IntPtr(LocalConn));
			SetOutput();
			Kcp.KcpNodelay(kcp, 1, 10, 1, 1);
			Kcp.KcpWndsize(kcp, 256, 256);
			Kcp.KcpSetmtu(kcp, 470);

			isConnected = true;
			lastRecvTime = GetService().TimeNow;

			HandleSend();
		}

		public void Accept()
		{
			if (socket == null)
			{
				return;
			}
			
			uint timeNow = GetService().TimeNow;

			try
			{
				byte[] buffer = memoryStream.GetBuffer();
				buffer.WriteTo(0, KcpProtocalType.ACK);
				buffer.WriteTo(1, LocalConn);
				buffer.WriteTo(5, RemoteConn);
				socket.SendTo(buffer, 0, 9, SocketFlags.None, remoteEndPoint);
				
				// 200毫秒后再次update发送connect请求
				GetService().AddToUpdateNextTime(timeNow + 200, Id);
			}
			catch (Exception e)
			{
				Log.Error(e);
				OnError(ErrorCode.ERR_SocketCantSend);
			}
		}

		/// <summary>
		/// 发送请求连接消息
		/// </summary>
		private void Connect()
		{
			try
			{
				uint timeNow = GetService().TimeNow;
				
				lastRecvTime = timeNow;
				
				byte[] buffer = memoryStream.GetBuffer();
				buffer.WriteTo(0, KcpProtocalType.SYN);
				buffer.WriteTo(1, LocalConn);
				socket.SendTo(buffer, 0, 5, SocketFlags.None, remoteEndPoint);
				
				// 200毫秒后再次update发送connect请求
				GetService().AddToUpdateNextTime(timeNow + 300, Id);
			}
			catch (Exception e)
			{
				Log.Error(e);
				OnError(ErrorCode.ERR_SocketCantSend);
			}
		}

		private void Disconnect()
		{
			if (socket == null)
			{
				return;
			}
			try
			{
				byte[] buffer = memoryStream.GetBuffer();
				buffer.WriteTo(0, KcpProtocalType.FIN);
				buffer.WriteTo(1, LocalConn);
				buffer.WriteTo(5, RemoteConn);
				buffer.WriteTo(9, (uint)Error);
				socket.SendTo(buffer, 0, 13, SocketFlags.None, remoteEndPoint);
			}
			catch (Exception e)
			{
				Log.Error(e);
				OnError(ErrorCode.ERR_SocketCantSend);
			}
		}

		public void Update()
		{
			if (IsDisposed)
			{
				return;
			}

			uint timeNow = GetService().TimeNow;
			
			// 如果还没连接上，发送连接请求
			if (!isConnected)
			{
				// 10秒没连接上则报错
				if (timeNow - createTime > 10 * 1000)
				{
					OnError(ErrorCode.ERR_KcpCantConnect);
					return;
				}
				
				if (timeNow - lastRecvTime < 500)
				{
					return;
				}

				switch (ChannelType)
				{
					case ChannelType.Accept:
						Accept();
						break;
					case ChannelType.Connect:
						Connect();
						break;
				}
				
				return;
			}

			// 超时断开连接
			//if (timeNow - lastRecvTime > 40 * 1000)
			//{
			//	OnError(ErrorCode.ERR_KcpChannelTimeout);
			//	return;
			//}

			try
			{
				Kcp.KcpUpdate(kcp, timeNow);
			}
			catch (Exception e)
			{
				Log.Error(e);
				OnError(ErrorCode.ERR_SocketError);
				return;
			}


			if (kcp != IntPtr.Zero)
			{
				uint nextUpdateTime = Kcp.KcpCheck(kcp, timeNow);
				GetService().AddToUpdateNextTime(nextUpdateTime, Id);
			}
		}

		private void HandleSend()
		{
			while (true)
			{
				if (sendBuffer.Count <= 0)
				{
					break;
				}

				WaitSendBuffer buffer = sendBuffer.Dequeue();
				KcpSend(buffer.Bytes, buffer.Length);
			}
		}

		public void HandleRecv(byte[] date, int offset, int length)
		{
			if (IsDisposed)
			{
				return;
			}

			isConnected = true;
			
			Kcp.KcpInput(kcp, date, offset, length);
			GetService().AddToUpdateNextTime(0, Id);

			while (true)
			{
				if (IsDisposed)
				{
					return;
				}
				int n = Kcp.KcpPeeksize(kcp);
				if (n < 0)
				{
					return;
				}
				if (n == 0)
				{
					OnError((int)SocketError.NetworkReset);
					return;
				}

				byte[] buffer = memoryStream.GetBuffer();
				memoryStream.SetLength(n);
				memoryStream.Seek(0, SeekOrigin.Begin);
				int count = Kcp.KcpRecv(kcp, buffer, ushort.MaxValue);
				if (n != count)
				{
					return;
				}
				if (count <= 0)
				{
					return;
				}

				lastRecvTime = GetService().TimeNow;

				OnRead(memoryStream);
			}
		}

		public void Output(IntPtr bytes, int count)
		{
			if (IsDisposed)
			{
				return;
			}
			try
			{
				if (count == 0)
				{
					Log.Error($"output 0");
					return;
				}

				byte[] buffer = memoryStream.GetBuffer();
				buffer.WriteTo(0, KcpProtocalType.MSG);
				// 每个消息头部写下该channel的id;
				buffer.WriteTo(1, LocalConn);
				Marshal.Copy(bytes, buffer, 5, count);
				socket.SendTo(buffer, 0, count + 5, SocketFlags.None, remoteEndPoint);
			}
			catch (Exception e)
			{
				Log.Error(e);
				OnError(ErrorCode.ERR_SocketCantSend);
			}
		}
		
#if !ENABLE_IL2CPP
		private KcpOutput kcpOutput;
#endif

		public void SetOutput()
		{
#if ENABLE_IL2CPP
			Kcp.KcpSetoutput(kcp, KcpOutput);
#else
			// 跟上一行一样写法，pc跟linux会出错, 保存防止被GC
			kcpOutput = KcpOutput;
			Kcp.KcpSetoutput(kcp, kcpOutput);
#endif
		}


#if ENABLE_IL2CPP
		[AOT.MonoPInvokeCallback(typeof(KcpOutput))]
#endif
		public static int KcpOutput(IntPtr bytes, int len, IntPtr kcp, IntPtr user)
        {
            KService.Output(bytes, len, user);
            return len;
        }

        private void KcpSend(byte[] buffers, int length)
		{
			if (IsDisposed)
			{
				return;
			}
			Kcp.KcpSend(kcp, buffers, length);
			GetService().AddToUpdateNextTime(0, Id);
		}

		private void Send(byte[] buffer, int index, int length)
		{
			if (isConnected)
			{
				KcpSend(buffer, length);
				return;
			}

			sendBuffer.Enqueue(new WaitSendBuffer(buffer, length));
		}

		public override void Send(MemoryStream stream)
		{
			if (kcp != IntPtr.Zero)
			{
				// 检查等待发送的消息，如果超出两倍窗口大小，应该断开连接
				if (Kcp.KcpWaitsnd(kcp) > 256 * 2)
				{
					OnError(ErrorCode.ERR_KcpWaitSendSizeTooLarge);
					return;
				}
			}

			ushort size = (ushort)(stream.Length - stream.Position);
			byte[] bytes;
			if (isConnected)
			{
				bytes = stream.GetBuffer();
			}
			else
			{
				bytes = new byte[size];
				Array.Copy(stream.GetBuffer(), stream.Position, bytes, 0, size);
			}

			Send(bytes, 0, size);
		}
	}
}
