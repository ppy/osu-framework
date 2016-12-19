// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Platform
{
    public class TcpIpcProvider : IDisposable
    {
        private readonly int ipcPort = 45356;
        private TcpListener listener;
        private CancellationTokenSource cancelListener;
        private CancellationToken token;

        public event Action<IpcMessage> MessageReceived;
    
        public bool Bind()
        {
            listener = new TcpListener(IPAddress.Loopback, ipcPort);
            try
            {
                listener.Start();
                cancelListener = new CancellationTokenSource();
                token = cancelListener.Token;
                return true;
            }
            catch (SocketException ex)
            {
                listener = null;
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    return false;
                else
                {
                    Console.WriteLine($@"Unhandled exception initializing IPC server: {ex}");
                    return false;
                }
            }
        }

        public async Task Start()
        {
            while (true)
            {
                while (!listener.Pending())
                {
                    await Task.Delay(10);
                    if (token.IsCancellationRequested)
                    {
                        listener.Stop();
                        return;
                    }
                }
                using (var client = await listener.AcceptTcpClientAsync())
                {
                    using (var stream = client.GetStream())
                    {
                        byte[] header = new byte[sizeof(int)];
                        await stream.ReadAsync(header, 0, sizeof(int));
                        int len = BitConverter.ToInt32(header, 0);
                        byte[] data = new byte[len];
                        await stream.ReadAsync(data, 0, len);
                        var str = Encoding.UTF8.GetString(data);
                        var json = JToken.Parse(str);
                        var type = Type.GetType(json["Type"].Value<string>());
                        Debug.Assert(type != null);
                        var msg = new IpcMessage
                        {
                            Type = type.AssemblyQualifiedName,
                            Value = JsonConvert.DeserializeObject(
                                json["Value"].ToString(), type),
                        };
                        MessageReceived?.Invoke(msg);
                    }
                }
            }
        }

        public async Task SendMessage(IpcMessage message)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, ipcPort);
                using (var stream = client.GetStream())
                {
                    var str = JsonConvert.SerializeObject(message, Formatting.None);
                    byte[] data = Encoding.UTF8.GetBytes(str);
                    byte[] header = BitConverter.GetBytes(data.Length);
                    await stream.WriteAsync(header, 0, header.Length);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                }
            }
        }

        public void Dispose()
        {
            if (listener != null)
                cancelListener.Cancel();
        }
    }
}

