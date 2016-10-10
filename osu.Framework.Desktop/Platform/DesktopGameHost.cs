// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        
        private readonly int ipcPort = 45356;
        private TcpListener listener;
        
        public override void Load()
        {
            listener = new TcpListener(IPAddress.Loopback, ipcPort);
            try
            {
                listener.Start();
                IsPrimaryInstance = true;
                runIPCServer();
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    IsPrimaryInstance = false;
                else
                    throw ex;
            }
            base.Load();
        }
        
        private async void runIPCServer()
        {
            IsPrimaryInstance = true;
            while (true)
            {
                using (var client = await listener.AcceptTcpClientAsync())
                using (var stream = client.GetStream())
                {
                    byte[] header = new byte[sizeof(int)];
                    await stream.ReadAsync(header, 0, sizeof(int));
                    int len = BitConverter.ToInt32(header, 0);
                    byte[] data = new byte[len];
                    await stream.ReadAsync(data, 0, len);
                    var str = Encoding.UTF8.GetString(data);
                    OnMessageReceived(JToken.Parse(str));
                }
            }
        }
        
        protected override async Task SendMessage(JToken message)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, ipcPort);
                using (var stream = client.GetStream())
                {
                    var str = message.ToString(Formatting.None);
                    byte[] header = BitConverter.GetBytes(str.Length);
                    await stream.WriteAsync(header, 0, header.Length);
                    byte[] data = Encoding.UTF8.GetBytes(str);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                }
            }
        }
    }
}
