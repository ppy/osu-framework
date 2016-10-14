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
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Platform
{
    public abstract class DesktopGameHost : BasicGameHost
    {
        public override GLControl GLControl => Window?.Form;

        private TextInputSource textInputBox;
        public override TextInputSource TextInput => textInputBox ?? (textInputBox = ((DesktopGameWindow)Window).CreateTextInput());
        private TcpIpcProvider IpcProvider;
        private Task IpcTask;
        
        public override void Load(BaseGame game)
        {
            IpcProvider = new TcpIpcProvider();
            IsPrimaryInstance = IpcProvider.Bind();
            if (IsPrimaryInstance)
            {
                IpcProvider.MessageReceived += msg => OnMessageReceived(msg);
                IpcTask = IpcProvider.Start();
            }
            base.Load(game);
        }
       
        
        protected override async Task SendMessage(IpcMessage message)
        {
            await IpcProvider.SendMessage(message);
        }
        
        protected override void Dispose(bool isDisposing)
        {
            IpcProvider.Dispose();
            IpcTask?.Wait(50);
            base.Dispose(isDisposing);
        }
    }
}
