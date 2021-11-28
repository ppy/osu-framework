// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace osu.Framework.Platform
{
    /// <summary>
    /// An inter-process communication provider that runs over a specified TCP port, binding to the loopback address.
    /// </summary>
    public class TcpIpcProvider : IDisposable
    {
        /// <summary>
        /// Invoked when a message is received by this IPC server.
        /// Returns either a response in the form of an <see cref="IpcMessage"/>, or <c>null</c> for no response.
        /// </summary>
        public event Func<IpcMessage, IpcMessage> MessageReceived;

        private TcpListener listener;
        private CancellationTokenSource cancelListener;

        private readonly int port;

        /// <summary>
        /// Create a new provider.
        /// </summary>
        /// <param name="port">The port to operate on.</param>
        public TcpIpcProvider(int port)
        {
            this.port = port;
        }

        /// <summary>
        /// Attempt to bind as the "server" instance.
        /// </summary>
        /// <returns>Whether the bind was successful. If <c>false</c>, another instance is likely already running (and can be messaged using <see cref="SendMessageAsync"/> or <see cref="SendMessageWithResponseAsync"/>).</returns>
        public bool Bind()
        {
            listener = new TcpListener(IPAddress.Loopback, port);

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

        /// <summary>
        /// Start processing events received by the listener. <see cref="Bind"/> must be called first.
        /// </summary>
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
                            var message = await receive(stream, token).ConfigureAwait(false);
                            if (message == null)
                                continue;

                            var response = MessageReceived?.Invoke(message);

                            if (response != null)
                                await send(stream, response).ConfigureAwait(false);
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

        /// <summary>
        /// Send a message to the IPC server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public async Task SendMessageAsync(IpcMessage message)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, port).ConfigureAwait(false);

                using (var stream = client.GetStream())
                    await send(stream, message).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send a message to the IPC server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response from the server.</returns>
        public async Task<IpcMessage> SendMessageWithResponseAsync(IpcMessage message)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, port).ConfigureAwait(false);

                using (var stream = client.GetStream())
                {
                    await send(stream, message).ConfigureAwait(false);
                    return await receive(stream).ConfigureAwait(false);
                }
            }
        }

        private async Task send(Stream stream, IpcMessage message)
        {
            string str = JsonConvert.SerializeObject(message, Formatting.None);
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] header = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(header.AsMemory()).ConfigureAwait(false);
            await stream.WriteAsync(data.AsMemory()).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private async Task<IpcMessage> receive(Stream stream, CancellationToken cancellationToken = default)
        {
            byte[] header = new byte[sizeof(int)];
            await stream.ReadAsync(header.AsMemory(), cancellationToken).ConfigureAwait(false);

            int len = BitConverter.ToInt32(header, 0);
            if (len == 0)
                return null;

            byte[] data = new byte[len];
            await stream.ReadAsync(data.AsMemory(), cancellationToken).ConfigureAwait(false);

            string str = Encoding.UTF8.GetString(data);

            var json = JToken.Parse(str);
            var type = Type.GetType(json["Type"].Value<string>());
            var value = json["Value"];

            Trace.Assert(type != null);
            Trace.Assert(value != null);

            return new IpcMessage
            {
                Type = type.AssemblyQualifiedName,
                Value = JsonConvert.DeserializeObject(value.ToString(), type),
            };
        }

        public void Dispose()
        {
            if (listener != null)
                cancelListener.Cancel();
        }
    }
}
