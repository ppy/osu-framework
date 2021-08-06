﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Configuration;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Touchpad;

namespace osu.Framework.Platform
{
    public abstract class DesktopGameHost : GameHost
    {
        private TcpIpcProvider ipcProvider;
        private readonly bool bindIPCPort;
        private Thread ipcThread;

        protected DesktopGameHost(string gameName = @"", bool bindIPCPort = false, bool portableInstallation = false)
            : base(gameName)
        {
            this.bindIPCPort = bindIPCPort;
            IsPortableInstallation = portableInstallation;
        }

        protected sealed override Storage GetDefaultGameStorage()
        {
            if (IsPortableInstallation || File.Exists(Path.Combine(RuntimeInfo.StartupDirectory, FrameworkConfigManager.FILENAME)))
                return GetStorage(RuntimeInfo.StartupDirectory);

            return base.GetDefaultGameStorage();
        }

        public sealed override Storage GetStorage(string path) => new DesktopStorage(path, this);

        protected override void SetupForRun()
        {
            if (bindIPCPort)
                startIPC();

            base.SetupForRun();
        }

        private void startIPC()
        {
            Debug.Assert(ipcProvider == null);

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

        public bool IsPortableInstallation { get; }

        public override bool CapsLockEnabled => (Window as SDL2DesktopWindow)?.CapsLockPressed == true;

        public override void OpenFileExternally(string filename) => openUsingShellExecute(filename);

        public override void OpenUrlExternally(string url) => openUsingShellExecute(url);

        private void openUsingShellExecute(string path) => Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true //see https://github.com/dotnet/corefx/issues/10361
        });

        public override ITextInputSource GetTextInput() => Window == null ? null : new GameWindowTextInput(Window);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new KeyboardHandler(),
#if NET5_0
                // tablet should get priority over mouse to correctly handle cases where tablet drivers report as mice as well.
                new Input.Handlers.Tablet.OpenTabletDriverHandler(),
#endif
                new TouchpadHandler(),
                new MouseHandler(),
                new JoystickHandler(),
                new MidiHandler(),
            };

        public override Task SendMessageAsync(IpcMessage message) => ipcProvider.SendMessageAsync(message);

        protected override void Dispose(bool isDisposing)
        {
            ipcProvider?.Dispose();
            ipcThread?.Join(50);
            base.Dispose(isDisposing);
        }
    }
}
