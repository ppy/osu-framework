// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Define an IPC channel which supports sending a specific well-defined type.
    /// </summary>
    /// <typeparam name="T">The type to send.</typeparam>
    public class IpcChannel<T> : IpcChannel<T, object> where T : class
    {
        public IpcChannel(IIpcHost host)
            : base(host)
        {
        }
    }

    /// <summary>
    /// Define an IPC channel which supports sending and receiving a specific well-defined type.
    /// </summary>
    /// <typeparam name="T">The type to send.</typeparam>
    /// <typeparam name="TResponse">The type to receive.</typeparam>
    public class IpcChannel<T, TResponse> : IDisposable
        where T : class
        where TResponse : class
    {
        private readonly IIpcHost host;

        public event Func<T, TResponse?>? MessageReceived;

        public IpcChannel(IIpcHost host)
        {
            this.host = host;
            this.host.MessageReceived += handleMessage;
        }

        public Task SendMessageAsync(T message) => host.SendMessageAsync(new IpcMessage
        {
            Type = typeof(T).AssemblyQualifiedName,
            Value = message,
        });

        public async Task<TResponse?> SendMessageWithResponseAsync(T message)
        {
            var response = await host.SendMessageWithResponseAsync(new IpcMessage
            {
                Type = typeof(T).AssemblyQualifiedName,
                Value = message,
            }).ConfigureAwait(false);

            if (response == null)
                return null;

            if (response.Type != typeof(TResponse).AssemblyQualifiedName)
                return null;

            return (TResponse)response.Value;
        }

        private IpcMessage? handleMessage(IpcMessage message)
        {
            if (message.Type != typeof(T).AssemblyQualifiedName)
                return null;

            var response = MessageReceived?.Invoke((T)message.Value);

            if (response == null)
                return null;

            return new IpcMessage
            {
                Type = typeof(TResponse).AssemblyQualifiedName,
                Value = response
            };
        }

        public void Dispose()
        {
            host.MessageReceived -= handleMessage;
        }
    }
}
