using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace Net
{
    public class WChannel: AChannel
    {
        public HttpListenerWebSocketContext WebSocketContext { get; }

        private readonly WebSocket webSocket;

        private readonly Queue<byte[]> queue = new Queue<byte[]>();

        private bool isSending;

        private bool isConnected;

        private readonly MemoryStream memoryStream;

        private readonly MemoryStream recvStream;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public WChannel(HttpListenerWebSocketContext webSocketContext, AService service): base(service, ChannelType.Accept)
        {
            WebSocketContext = webSocketContext;

            webSocket = webSocketContext.WebSocket;

            memoryStream = GetService().MemoryStreamManager.GetStream("message", ushort.MaxValue);
            recvStream = GetService().MemoryStreamManager.GetStream("message", ushort.MaxValue);

            isConnected = true;
        }

        public WChannel(WebSocket webSocket, AService service): base(service, ChannelType.Connect)
        {
            this.webSocket = webSocket;

            memoryStream = GetService().MemoryStreamManager.GetStream("message", ushort.MaxValue);
            recvStream = GetService().MemoryStreamManager.GetStream("message", ushort.MaxValue);

            isConnected = false;
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Dispose();

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            webSocket.Dispose();

            memoryStream.Dispose();
        }

        public override MemoryStream Stream
        {
            get
            {
                return memoryStream;
            }
        }

        public override void Start()
        {
            if (!isConnected)
            {
                return;
            }

            StartRecv();
            StartSend();
        }

        private WService GetService()
        {
            return (WService) Service;
        }

        public async void ConnectAsync(string url)
        {
            try
            {
                await ((ClientWebSocket) webSocket).ConnectAsync(new Uri(url), cancellationTokenSource.Token);
                isConnected = true;
                Start();
            }
            catch (Exception e)
            {
                Log.Error(e);
                OnError(ErrorCode.ERR_WebsocketConnectError);
            }
        }

        public override void Send(MemoryStream stream)
        {
            byte[] bytes = new byte[stream.Length];
            Array.Copy(stream.GetBuffer(), bytes, bytes.Length);
            queue.Enqueue(bytes);

            if (isConnected)
            {
                StartSend();
            }
        }

        public async void StartSend()
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (isSending)
                {
                    return;
                }

                isSending = true;

                while (true)
                {
                    if (queue.Count == 0)
                    {
                        isSending = false;
                        return;
                    }

                    byte[] bytes = queue.Dequeue();
                    try
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Binary, true, cancellationTokenSource.Token);
                        if (IsDisposed)
                        {
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        OnError(ErrorCode.ERR_WebsocketSendError);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public async void StartRecv()
        {

            try
            {
                while (true)
                {
#if SERVER
                    ValueWebSocketReceiveResult receiveResult;
#else
                    WebSocketReceiveResult receiveResult;
#endif
                    int receiveCount = 0;
                    do
                    {
#if SERVER
                        receiveResult = await webSocket.ReceiveAsync(
                            new Memory<byte>(recvStream.GetBuffer(), receiveCount, recvStream.Capacity - receiveCount),
                            cancellationTokenSource.Token);
#else
                        receiveResult = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(recvStream.GetBuffer(), receiveCount, recvStream.Capacity - receiveCount), 
                            cancellationTokenSource.Token);
#endif

                        receiveCount += receiveResult.Count;
                    }
                    while (!receiveResult.EndOfMessage);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        OnError(ErrorCode.ERR_WebsocketPeerReset);
                        return;
                    }

                    if (receiveResult.Count > ushort.MaxValue)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, $"message too big: {receiveResult.Count}",
                            cancellationTokenSource.Token);
                        OnError(ErrorCode.ERR_WebsocketMessageTooBig);
                        return;
                    }

                    recvStream.SetLength(receiveResult.Count);
                    OnRead(recvStream);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                OnError(ErrorCode.ERR_WebsocketRecvError);
            }
        }
    }
}