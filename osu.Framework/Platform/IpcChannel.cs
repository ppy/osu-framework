// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    public class IpcChannel<T> : IDisposable
        where T : class
    {
        private readonly IIpcHost host;
        public event Func<T, T?>? MessageReceived;

        public IpcChannel(IIpcHost host)
        {
            this.host = host;
            this.host.MessageReceived += handleMessage;
        }

        public Task SendMessageAsync(T message) => host.SendMessageAsync(makeMessage(message));

        public async Task<T?> SendMessageWithResponseAsync(T message)
        {
            var response = await host.SendMessageWithResponseAsync(makeMessage(message)).ConfigureAwait(false);

            if (response == null || response.Type != typeof(T).AssemblyQualifiedName)
                return null;

            return (T)response.Value;
        }

        private IpcMessage makeMessage(T message) => new IpcMessage
        {
            Type = typeof(T).AssemblyQualifiedName,
            Value = message,
        };

        private IpcMessage? handleMessage(IpcMessage message)
        {
            if (message.Type != typeof(T).AssemblyQualifiedName)
                return null;

            var response = MessageReceived?.Invoke((T)message.Value);

            if (response == null)
                return null;

            return makeMessage(response);
        }

        public void Dispose()
        {
            host.MessageReceived -= handleMessage;
        }
    }
}
