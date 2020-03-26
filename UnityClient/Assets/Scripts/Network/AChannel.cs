using System;
using System.IO;

namespace Net
{
    public enum ChannelType
    {
        Connect,
        Accept,
    }

    public abstract class AChannel: IDisposable
    {
        public ChannelType ChannelType { get; }

        public AService Service { get; }

        public abstract MemoryStream Stream { get; }
		
        public int Error { get; set; }

        public string RemoteAddress { get; protected set; }

        public long Id { get; protected set; }
        public bool IsDisposed { get; private set; }

        public virtual void Start()
        {
        }

        private Action<AChannel, int> errorCallback;
		
        public event Action<AChannel, int> ErrorCallback
        {
            add
            {
                errorCallback += value;
            }
            remove
            {
                errorCallback -= value;
            }
        }
		
        private Action<MemoryStream> readCallback;

        public event Action<MemoryStream> ReadCallback
        {
            add
            {
                readCallback += value;
            }
            remove
            {
                readCallback -= value;
            }
        }
		
        protected void OnRead(MemoryStream memoryStream)
        {
            readCallback.Invoke(memoryStream);
        }

        protected void OnError(int e)
        {
            Error = e;
            errorCallback?.Invoke(this, e);
        }

        protected AChannel(AService service, ChannelType channelType)
        {
            Id = IdGenerater.GenerateId();
            ChannelType = channelType;
            Service = service;
        }
		
        public abstract void Send(MemoryStream stream);
		
        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Service.Remove(Id);
            }
        }
    }
}