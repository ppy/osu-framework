// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using OpenTK;
using GLControl = osu.Framework.Platform.GLControl;

namespace osu.Framework.Desktop.Platform
{
    public abstract class DesktopGameHost : BasicGameHost
    {
        public override GLControl GLControl => Window?.Form;

        private TextInputSource textInputBox;
        public override TextInputSource TextInput => textInputBox ?? (textInputBox = ((DesktopGameWindow)Window).CreateTextInput());

        public bool ListenForIpc = true;

        private TcpIpcProvider IpcProvider;
        private Task IpcTask;
        
        public override void Load(BaseGame game)
        {
            if (ListenForIpc)
            {
                IpcProvider = new TcpIpcProvider();
                IsPrimaryInstance = IpcProvider.Bind();
                if (IsPrimaryInstance)
                {
                    IpcProvider.MessageReceived += msg => OnMessageReceived(msg);
                    IpcTask = IpcProvider.Start();
                }
            }
            base.Load(game);
        }

        protected override void LoadGame(BaseGame game)
        {
            //delay load until we have a size.
            queueLoad(game);
        }

        private void queueLoad(BaseGame game)
        {
            UpdateScheduler.Add(delegate
            {
                if (Size == Vector2.Zero)
                {
                    queueLoad(game);
                    return;
                }

                base.LoadGame(game);
            });
        }


        protected override async Task SendMessage(IpcMessage message)
        {
            await IpcProvider.SendMessage(message);
        }
        
        protected override void Dispose(bool isDisposing)
        {
            IpcProvider?.Dispose();
            IpcTask?.Wait(50);
            base.Dispose(isDisposing);
        }
    }
}
