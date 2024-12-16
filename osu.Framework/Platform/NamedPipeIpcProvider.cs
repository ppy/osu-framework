// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Extensions;
using osu.Framework.Logging;

namespace osu.Framework.Platform
{
    /// <summary>
    /// An inter-process communication provider that runs over a specified named pipe.
    /// This single class handles both binding as a server, or messaging another bound instance that is acting as a server.
    /// </summary>
    public class NamedPipeIpcProvider : IDisposable
    {
        /// <summary>
        /// Invoked when a message is received when running as a server.
        /// Returns either a response in the form of an <see cref="IpcMessage"/>, or <c>null</c> for no response.
        /// </summary>
        public event Func<IpcMessage, IpcMessage?>? MessageReceived;

        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();

        private readonly string pipeName;

        private Task? listenTask;

        private NamedPipeServerStream? pipe;

        private Mutex? mutex;

        /// <summary>
        /// Create a new provider.
        /// </summary>
        /// <param name="pipeName">The port to operate on.</param>
        public NamedPipeIpcProvider(string pipeName)
        {
            this.pipeName = pipeName;
        }

        /// <summary>
        /// Attempt to bind to the named pipe as a server, and start listening for incoming connections if successful.
        /// </summary>
        /// <returns>
        /// Whether the bind was successful.
        /// If <c>false</c>, another instance is likely already running (and can be messaged using <see cref="SendMessageAsync"/> or <see cref="SendMessageWithResponseAsync"/>).
        /// </returns>
        public bool Bind()
        {
            if (pipe != null)
                throw new InvalidOperationException($"Can't {nameof(Bind)} more than once.");

            try
            {
                string name = $"osu-framework-{pipeName}";

                // Named pipes from different processes are allowed to coexist, but we don't want this for our purposes.
                // Using a system global mutex allows ensuring that only one osu!framework project using the same pipe name
                // will be able to bind.
                mutex = new Mutex(false, $"Global\\{name}", out bool createdNew);

                if (!createdNew)
                    return false;

                pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1);

                listenTask = listen(pipe);

                return true;
            }
            catch (IOException ex)
            {
                Logger.Error(ex, "Unable to bind IPC server");
                return false;
            }
        }

        private async Task listen(NamedPipeServerStream pipe)
        {
            var token = cancellationSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);

                        var message = await receive(pipe, token).ConfigureAwait(false);

                        if (message == null)
                            continue;

                        var response = MessageReceived?.Invoke(message);

                        if (response != null)
                            await send(pipe, response).ConfigureAwait(false);

                        pipe.Disconnect();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Error handling incoming IPC request.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                try
                {
                    if (pipe.IsConnected)
                        pipe.Disconnect();
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
            using (var client = new NamedPipeClientStream($"osu-framework-{pipeName}"))
            {
                await client.ConnectAsync().ConfigureAwait(false);
                await send(client, message).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send a message to the IPC server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response from the server.</returns>
        public async Task<IpcMessage?> SendMessageWithResponseAsync(IpcMessage message)
        {
            using (var client = new NamedPipeClientStream($"osu-framework-{pipeName}"))
            {
                await client.ConnectAsync().ConfigureAwait(false);
                await send(client, message).ConfigureAwait(false);
                return await receive(client).ConfigureAwait(false);
            }
        }

        private static async Task send(Stream stream, IpcMessage message)
        {
            string str = JsonConvert.SerializeObject(message, Formatting.None);
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] header = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(header.AsMemory()).ConfigureAwait(false);
            await stream.WriteAsync(data.AsMemory()).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private static async Task<IpcMessage?> receive(Stream stream, CancellationToken cancellationToken = default)
        {
            const int header_length = sizeof(int);

            byte[] header = new byte[header_length];

            int read = await stream.ReadAsync(header.AsMemory(), cancellationToken).ConfigureAwait(false);

            if (read < header_length)
                return null;

            int contentLength = BitConverter.ToInt32(header, 0);

            if (contentLength == 0)
                return null;

            byte[] data = await stream.ReadBytesToArrayAsync(contentLength, cancellationToken).ConfigureAwait(false);

            string str = Encoding.UTF8.GetString(data);

            var json = JToken.Parse(str);

            string? typeName = json["Type"]?.Value<string>();

            if (typeName == null) throw new InvalidOperationException("Response JSON has missing Type field.");

            var type = Type.GetType(typeName);
            var value = json["Value"];

            if (type == null) throw new InvalidOperationException($"Response type could not be mapped ({typeName}).");
            if (value == null) throw new InvalidOperationException("Response JSON has missing Value field.");

            return new IpcMessage
            {
                Type = type.AssemblyQualifiedName,
                Value = JsonConvert.DeserializeObject(value.ToString(), type),
            };
        }

        public void Dispose()
        {
            const int thread_join_timeout = 2000;

            cancellationSource.Cancel();

            mutex?.Dispose();

            if (listenTask != null)
            {
                try
                {
                    listenTask.Wait(thread_join_timeout);
                    pipe?.Dispose();
                }
                catch
                {
                    Logger.Log($"IPC thread failed to exit in allocated time ({thread_join_timeout}ms).", LoggingTarget.Runtime, LogLevel.Important);
                }
            }
        }
    }
}
