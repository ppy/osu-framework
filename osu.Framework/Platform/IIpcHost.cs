// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    public interface IIpcHost
    {
        /// <summary>
        /// Invoked when a message is received by this IPC server.
        /// Returns either a response in the form of an <see cref="IpcMessage"/>, or <c>null</c> for no response.
        /// </summary>
        event Func<IpcMessage, IpcMessage?>? MessageReceived;

        /// <summary>
        /// Send a message to the IPC server.
        /// </summary>
        /// <param name="ipcMessage">The message to send.</param>
        Task SendMessageAsync(IpcMessage ipcMessage);

        /// <summary>
        /// Send a message to the IPC server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response from the server.</returns>
        public Task<IpcMessage?> SendMessageWithResponseAsync(IpcMessage message);
    }
}
