// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Logging;

namespace osu.Framework.Platform
{
    public abstract class DesktopGameHost : GameHost
    {
        private readonly TcpIpcProvider ipcProvider;
        private readonly Task ipcTask;

        protected DesktopGameHost(string gameName = @"", bool bindIPCPort = false)
            : base(gameName)
        {
            //todo: yeah.
            Architecture.SetIncludePath();

            foreach (string a in Environment.GetCommandLineArgs())
            {
                switch (a)
                {
                    case @"--reload-on-change":
                        ensureShadowCopy();
                        break;
                }
            }

            if (bindIPCPort)
            {
                ipcProvider = new TcpIpcProvider();
                IsPrimaryInstance = ipcProvider.Bind();
                if (IsPrimaryInstance)
                {
                    ipcProvider.MessageReceived += OnMessageReceived;
                    ipcTask = Task.Factory.StartNew(ipcProvider.StartAsync, TaskCreationOptions.LongRunning);
                }
            }

            Logger.Storage = Storage.GetStorageForDirectory("logs");
        }

        /// <summary>
        /// Copy ourselves to a temporary path and watch for updates to the original assembly.
        /// </summary>
        private void ensureShadowCopy()
        {
            string exe = System.Reflection.Assembly.GetEntryAssembly().Location;

            Debug.Assert(exe != null);

            // ReSharper disable once PossibleNullReferenceException
            if (exe.Contains(@"_shadow"))
            {
                //we are already running a shadow copy. monitor the original executable path for changes.
                exe = exe.Replace(@"_shadow", @"");

                DateTime originalTime = new FileInfo(exe).LastWriteTimeUtc;

                Task.Run(() =>
                {
                    while (new FileInfo(exe).LastWriteTimeUtc == originalTime)
                        Thread.Sleep(1000);

                    Process.Start(exe, @"--reload-on-change");
                    Environment.Exit(0);
                });

                return;
            }

            string shadowExe = exe.Replace(@".exe", @"_shadow.exe");

            int attempts = 5;
            while (attempts-- > 0)
            {
                try
                {
                    File.Copy(exe, shadowExe, true);
                    break;
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }

            Process.Start(shadowExe, @"--reload-on-change");
            Environment.Exit(0);
        }

        public override ITextInputSource GetTextInput() => Window == null ? null : new GameWindowTextInput(Window);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            var defaultEnabled = new InputHandler[]
            {
                new OpenTKMouseHandler(),
                new OpenTKKeyboardHandler(),
                new OpenTKJoystickHandler(),
            };

            var defaultDisabled = new InputHandler[]
            {
                new OpenTKRawMouseHandler(),
            };

            foreach (var h in defaultDisabled)
                h.Enabled.Value = false;

            return defaultEnabled.Concat(defaultDisabled);
        }

        public override async Task SendMessageAsync(IpcMessage message)
        {
            await ipcProvider.SendMessageAsync(message);
        }

        protected override void Dispose(bool isDisposing)
        {
            ipcProvider?.Dispose();
            ipcTask?.Wait(50);
            base.Dispose(isDisposing);
        }
    }
}
