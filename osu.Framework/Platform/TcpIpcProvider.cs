// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
                        await Task.Delay(10, token).ConfigureAwait(false);
                        if (token.IsCancellationRequested)
                            return;
                    }

                    using (var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false))
                    {
                        using (var stream = client.GetStream())
                        {
                            byte[] header = new byte[sizeof(int)];
                            await stream.ReadAsync(header.AsMemory(), token).ConfigureAwait(false);
                            int len = BitConverter.ToInt32(header, 0);
                            byte[] data = new byte[len];
                            await stream.ReadAsync(data.AsMemory(), token).ConfigureAwait(false);
                            string str = Encoding.UTF8.GetString(data);
                            var json = JToken.Parse(str);

                            var type = Type.GetType(json["Type"].Value<string>());
                            var value = json["Value"];

                            Trace.Assert(type != null);
                            Trace.Assert(value != null);

                            var msg = new IpcMessage
                            {
                                Type = type.AssemblyQualifiedName,
                                Value = JsonConvert.DeserializeObject(value.ToString(), type),
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
                await client.ConnectAsync(IPAddress.Loopback, ipc_port).ConfigureAwait(false);

                using (var stream = client.GetStream())
                {
                    string str = JsonConvert.SerializeObject(message, Formatting.None);
                    byte[] data = Encoding.UTF8.GetBytes(str);
                    byte[] header = BitConverter.GetBytes(data.Length);
                    await stream.WriteAsync(header.AsMemory()).ConfigureAwait(false);
                    await stream.WriteAsync(data.AsMemory()).ConfigureAwait(false);
                    await stream.FlushAsync().ConfigureAwait(false);
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
