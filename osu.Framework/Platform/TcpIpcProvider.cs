// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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

namespace osu.Framework.Platform
{
    public class TcpIpcProvider : IDisposable
    {
        private const int ipc_port = 45356;

        private TcpListener listener;
        private CancellationTokenSource cancelListener;

        public event Action<IpcMessage> MessageReceived;

        public bool Bind()
        {
            listener = new TcpListener(IPAddress.Loopback, ipc_port);
            try
            {
                listener.Start();
                cancelListener = new CancellationTokenSource();
                return true;
            }
            catch (SocketException ex)
            {
                listener = null;
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    return false;

                Console.WriteLine($@"Unhandled exception initializing IPC server: {ex}");
                return false;
            }
        }

        public async Task StartAsync()
        {
            var token = cancelListener.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (!listener.Pending())
                    {
                        await Task.Delay(10, token);
                        if (token.IsCancellationRequested)
                            return;
                    }

                    using (var client = await listener.AcceptTcpClientAsync())
                    {
                        using (var stream = client.GetStream())
                        {
                            byte[] header = new byte[sizeof(int)];
                            await stream.ReadAsync(header, 0, sizeof(int), token);
                            int len = BitConverter.ToInt32(header, 0);
                            byte[] data = new byte[len];
                            await stream.ReadAsync(data, 0, len, token);
                            var str = Encoding.UTF8.GetString(data);
                            var json = JToken.Parse(str);
                            var type = Type.GetType(json["Type"].Value<string>());
                            Trace.Assert(type != null);
                            var msg = new IpcMessage
                            {
                                // ReSharper disable once PossibleNullReferenceException
                                Type = type.AssemblyQualifiedName,
                                Value = JsonConvert.DeserializeObject(
                                    json["Value"].ToString(), type),
                            };
                            MessageReceived?.Invoke(msg);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                try
                {
                    listener.Stop();
                }
                catch
                {
                }
            }
        }

        public async Task SendMessageAsync(IpcMessage message)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, ipc_port);
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
