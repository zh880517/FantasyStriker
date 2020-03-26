using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.IO;

namespace Net
{
	public sealed class TService : AService
	{
		private readonly Dictionary<long, TChannel> idChannels = new Dictionary<long, TChannel>();

		private readonly SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
		private Socket acceptor;
		
		public RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();
		
		public List<long> needStartSendChannel = new List<long>();
		
		public int PacketSizeLength { get; }
		
		/// <summary>
		/// 即可做client也可做server
		/// </summary>
		public TService(int packetSizeLength, IPEndPoint ipEndPoint, Action<AChannel> acceptCallback)
		{
			PacketSizeLength = packetSizeLength;
			AcceptCallback += acceptCallback;
			
			acceptor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			acceptor.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			innArgs.Completed += OnComplete;
			
			acceptor.Bind(ipEndPoint);
			acceptor.Listen(1000);

			AcceptAsync();
		}

		public TService(int packetSizeLength)
		{
			PacketSizeLength = packetSizeLength;
		}
		
		public override void Dispose()
		{
            if (IsDisposed)
                return;
			foreach (long id in idChannels.Keys.ToArray())
			{
				TChannel channel = idChannels[id];
				channel.Dispose();
			}
			acceptor?.Close();
			acceptor = null;
			innArgs.Dispose();
		}

		private void OnComplete(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Accept:
					OneThreadSynchronizationContext.Instance.Post(OnAcceptComplete, e);
					break;
				default:
					throw new Exception($"socket accept error: {e.LastOperation}");
			}
		}
		
		public void AcceptAsync()
		{
			innArgs.AcceptSocket = null;
			if (acceptor.AcceptAsync(innArgs))
			{
				return;
			}
			OnAcceptComplete(innArgs);
		}

		private void OnAcceptComplete(object o)
		{
			if (acceptor == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
			
			if (e.SocketError != SocketError.Success)
			{
				Log.Error($"accept error {e.SocketError}");
				AcceptAsync();
				return;
			}
			TChannel channel = new TChannel(e.AcceptSocket, this);
			idChannels[channel.Id] = channel;
			
			try
			{
				OnAccept(channel);
			}
			catch (Exception exception)
			{
				Log.Error(exception);
			}

			if (acceptor == null)
			{
				return;
			}
			
			AcceptAsync();
		}
		
		public override AChannel GetChannel(long id)
		{
			TChannel channel = null;
			idChannels.TryGetValue(id, out channel);
			return channel;
		}

		public override AChannel ConnectChannel(IPEndPoint ipEndPoint)
		{
			TChannel channel = new TChannel(ipEndPoint, this);
			idChannels[channel.Id] = channel;
			return channel;
		}

		public override AChannel ConnectChannel(string address)
		{
			IPEndPoint ipEndPoint = NetworkHelper.ToIPEndPoint(address);
			return ConnectChannel(ipEndPoint);
		}

		public void MarkNeedStartSend(long id)
		{
			needStartSendChannel.Add(id);
		}

		public override void Remove(long id)
		{
			TChannel channel;
			if (!idChannels.TryGetValue(id, out channel))
			{
				return;
			}
			if (channel == null)
			{
				return;
			}
			idChannels.Remove(id);
			channel.Dispose();
		}

		public override void Update()
		{
			foreach (long id in needStartSendChannel)
			{
				TChannel channel;
				if (!idChannels.TryGetValue(id, out channel))
				{
					continue;
				}

				if (channel.IsSending)
				{
					continue;
				}

				try
				{
					channel.StartSend();
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
			
			needStartSendChannel.Clear();
		}
	}
}