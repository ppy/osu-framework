﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    public class IpcChannel<T> : IDisposable
    {
        private readonly IIpcHost host;
        public event Action<T> MessageReceived;

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

        private void handleMessage(IpcMessage message)
        {
            if (message.Type != typeof(T).AssemblyQualifiedName)
                return;
            MessageReceived?.Invoke((T)message.Value);
        }

        public void Dispose()
        {
            host.MessageReceived -= handleMessage;
        }
    }
}
