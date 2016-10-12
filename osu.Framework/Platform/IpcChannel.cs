using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace osu.Framework.Platform
{
    public class IpcChannel<T>
    {
        private BasicGameHost host;
        public event Action<T> MessageReceived;
    
        public IpcChannel(BasicGameHost host)
        {
            this.host = host;
            this.host.MessageReceived += HandleMessage;
        }
        public async Task SendMessage(T message)
        {
            var msg = new IpcMessage
            {
                Type = typeof(T).AssemblyQualifiedName,
                Value = message,
            };
            await host.SendMessage(msg);
        }
        private void HandleMessage(IpcMessage message)
        {
            if (message.Type != typeof(T).AssemblyQualifiedName)
                return;
            MessageReceived?.Invoke((T)message.Value);
        }
    }
}