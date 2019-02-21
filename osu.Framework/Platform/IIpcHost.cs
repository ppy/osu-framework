// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    public interface IIpcHost
    {
        event Action<IpcMessage> MessageReceived;

        Task SendMessageAsync(IpcMessage ipcMessage);
    }
}
