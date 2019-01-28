﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Logging;
using osuTK;

namespace osu.Framework.Platform
{
    public abstract class DesktopGameHost : GameHost
    {
        private readonly TcpIpcProvider ipcProvider;
        private readonly Thread ipcThread;

        protected DesktopGameHost(string gameName = @"", bool bindIPCPort = false, ToolkitOptions toolkitOptions = default)
            : base(gameName, toolkitOptions)
        {
            //todo: yeah.
            Architecture.SetIncludePath();

            if (bindIPCPort)
            {
                ipcProvider = new TcpIpcProvider();
                IsPrimaryInstance = ipcProvider.Bind();

                if (IsPrimaryInstance)
                {
                    ipcProvider.MessageReceived += OnMessageReceived;

                    ipcThread = new Thread(() => ipcProvider.StartAsync().Wait())
                    {
                        Name = "IPC",
                        IsBackground = true
                    };

                    ipcThread.Start();
                }
            }

            Logger.Storage = Storage.GetStorageForDirectory("logs");
        }

        public override void OpenFileExternally(string filename) => openUsingShellExecute(filename);

        public override void OpenUrlExternally(string url) => openUsingShellExecute(url);

        private void openUsingShellExecute(string path) => Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true //see https://github.com/dotnet/corefx/issues/10361
        });

        public override ITextInputSource GetTextInput() => Window == null ? null : new GameWindowTextInput(Window);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            var defaultEnabled = new InputHandler[]
            {
                new OsuTKMouseHandler(),
                new OsuTKKeyboardHandler(),
                new OsuTKJoystickHandler(),
            };

            var defaultDisabled = new InputHandler[]
            {
                new OsuTKRawMouseHandler(),
            };

            foreach (var h in defaultDisabled)
                h.Enabled.Value = false;

            return defaultEnabled.Concat(defaultDisabled);
        }

        public override Task SendMessageAsync(IpcMessage message) => ipcProvider.SendMessageAsync(message);

        protected override void Dispose(bool isDisposing)
        {
            ipcProvider?.Dispose();
            ipcThread?.Join(50);
            base.Dispose(isDisposing);
        }
    }
}
