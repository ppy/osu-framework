// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Mouse;

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

        public override bool IsPrimaryInstance
        {
            get
            {
                // make sure we have actually attempted to bind IPC as this call may occur before the host is run.
                ensureIPCReady();

                return base.IsPrimaryInstance;
            }
        }

        protected override void SetupForRun()
        {
            ensureIPCReady();

            base.SetupForRun();
        }

        private void ensureIPCReady()
        {
            if (!bindIPCPort)
                return;

            if (ipcProvider != null)
                return;

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

        public override void PresentFileExternally(string filename)
            // should be overriden to highlight/select the file in the folder if such native API exists.
            => OpenFileExternally(Path.GetDirectoryName(filename.TrimDirectorySeparator()));

        private void openUsingShellExecute(string path) => Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true //see https://github.com/dotnet/corefx/issues/10361
        });

        public override ITextInputSource GetTextInput() => Window == null ? null : new SDL2DesktopWindowTextInput(Window as SDL2DesktopWindow);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new KeyboardHandler(),
#if NET5_0
                // tablet should get priority over mouse to correctly handle cases where tablet drivers report as mice as well.
                new Input.Handlers.Tablet.OpenTabletDriverHandler(),
#endif
                new MouseHandler(),
                new JoystickHandler(),
                new MidiHandler(),
            };

        public override Task SendMessageAsync(IpcMessage message)
        {
            ensureIPCReady();

            return ipcProvider.SendMessageAsync(message);
        }

        protected override void Dispose(bool isDisposing)
        {
            ipcProvider?.Dispose();
            ipcThread?.Join(50);
            base.Dispose(isDisposing);
        }
    }
}
