using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using Microsoft.IO;

namespace Net
{
    public class WService: AService
    {
        private readonly HttpListener httpListener;
        
        private readonly Dictionary<long, WChannel> channels = new Dictionary<long, WChannel>();
        
        public RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        public WService(IEnumerable<string> prefixs, Action<AChannel> acceptCallback)
        {
            AcceptCallback += acceptCallback;
            
            httpListener = new HttpListener();

            StartAccept(prefixs);
        }
        
        public WService()
        {
        }
        
        public override AChannel GetChannel(long id)
        {
            WChannel channel;
            channels.TryGetValue(id, out channel);
            return channel;
        }

        public override AChannel ConnectChannel(IPEndPoint ipEndPoint)
        {
            throw new NotImplementedException();
        }

        public override AChannel ConnectChannel(string address)
        {
			ClientWebSocket webSocket = new ClientWebSocket();
            WChannel channel = new WChannel(webSocket, this);
            channels[channel.Id] = channel;
            channel.ConnectAsync(address);
            return channel;
        }

        public override void Remove(long id)
        {
            WChannel channel;
            if (!channels.TryGetValue(id, out channel))
            {
                return;
            }

            channels.Remove(id);
            channel.Dispose();
        }

        public override void Update()
        {
            
        }

        public async void StartAccept(IEnumerable<string> prefixs)
        {
            try
            {
                foreach (string prefix in prefixs)
                {
                    httpListener.Prefixes.Add(prefix);
                }
                
                httpListener.Start();

                while (true)
                {
                    try
                    {
                        HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();

                        HttpListenerWebSocketContext webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);

                        WChannel channel = new WChannel(webSocketContext, this);

                        channels[channel.Id] = channel;

                        OnAccept(channel);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == 5)
                {
                    throw new Exception($"CMD管理员中输入: netsh http add urlacl url=http://*:8080/ user=Everyone", e);
                }

                Log.Error(e);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}