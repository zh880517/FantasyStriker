using System;
using System.Net;

namespace Net
{
	public enum NetworkProtocol
	{
		KCP,
		TCP,
		WebSocket,
	}

	public abstract class AService : IDisposable
	{
        public bool IsDisposed { get; private set; }
        public abstract AChannel GetChannel(long id);

		private Action<AChannel> acceptCallback;

		public event Action<AChannel> AcceptCallback
		{
			add
			{
				acceptCallback += value;
			}
			remove
			{
				acceptCallback -= value;
			}
		}
		
		protected void OnAccept(AChannel channel)
		{
			acceptCallback.Invoke(channel);
		}

		public abstract AChannel ConnectChannel(IPEndPoint ipEndPoint);
		
		public abstract AChannel ConnectChannel(string address);

		public abstract void Remove(long channelId);

		public abstract void Update();

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }
}