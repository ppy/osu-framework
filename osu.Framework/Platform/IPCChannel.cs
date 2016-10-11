using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace osu.Framework.Platform
{
    public class IPCChannel<T>
    {
        private BasicGameHost host;
        public event Action<T> MessageReceived;
    
        public IPCChannel(BasicGameHost host)
        {
            this.host = host;
            this.host.MessageReceived += HandleMessage;
        }
        public async Task SendMessage(T message)
        {
            var msg = new IPCMessage
            {
                Type = typeof(T).AssemblyQualifiedName,
                Value = message,
            };
            await host.SendMessage(msg);
        }
        private void HandleMessage(IPCMessage message)
        {
            if (message.Type != typeof(T).AssemblyQualifiedName)
                return;
            MessageReceived?.Invoke((T)message.Value);
        }
    }
}