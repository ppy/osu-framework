// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Touch;

namespace osu.Framework.Platform
{
    public abstract class DesktopGameHost : GameHost
    {
        public const int IPC_PORT = 45356;

        private TcpIpcProvider ipcProvider;
        private readonly bool bindIPCPort;

        protected DesktopGameHost(string gameName, HostOptions options = null)
            : base(gameName, options)
        {
            bindIPCPort = Options.BindIPC;
            IsPortableInstallation = Options.PortableInstallation;
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

            ipcProvider = new TcpIpcProvider(IPC_PORT);
            ipcProvider.MessageReceived += OnMessageReceived;

            IsPrimaryInstance = ipcProvider.Bind();
        }

        public bool IsPortableInstallation { get; }

        public override bool CapsLockEnabled => (Window as SDL2DesktopWindow)?.CapsLockPressed == true;

        public override bool OpenFileExternally(string filename)
        {
            openUsingShellExecute(filename);
            return true;
        }

        public override void OpenUrlExternally(string url) => openUsingShellExecute(url);

        public override bool PresentFileExternally(string filename)
        {
            // should be overriden to highlight/select the file in the folder if such native API exists.
            OpenFileExternally(Path.GetDirectoryName(filename.TrimDirectorySeparator()));
            return true;
        }

        private void openUsingShellExecute(string path) => Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true //see https://github.com/dotnet/corefx/issues/10361
        });

        protected override TextInputSource CreateTextInput()
        {
            if (Window is SDL2DesktopWindow desktopWindow)
                return new SDL2DesktopWindowTextInput(desktopWindow);

            return base.CreateTextInput();
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new KeyboardHandler(),
#if NET6_0_OR_GREATER
                // tablet should get priority over mouse to correctly handle cases where tablet drivers report as mice as well.
                new Input.Handlers.Tablet.OpenTabletDriverHandler(),
#endif
                new MouseHandler(),
                new TouchHandler(),
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
            base.Dispose(isDisposing);
        }
    }
}
