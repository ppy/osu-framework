using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace osu.Framework.Platform
{
    public class IPCHost<T>
    {
        private BasicGameHost host;
        public event Action<T> MessageReceived;
    
        public IPCHost(BasicGameHost host)
        {
            this.host = host;
            this.host.MessageReceived += HandleMessage;
        }
        public async Task SendMessage(T message)
        {
            var json = new JObject();
            json["Type"] = typeof(T).FullName;
            json["Value"] = JToken.Parse(JsonConvert.SerializeObject(message));
            await host.SendMessage(json);
        }
        private void HandleMessage(JToken message)
        {
            if (message["Type"].Value<string>() != typeof(T).FullName)
                return;
            var val = message["Value"];
            var obj = JsonConvert.DeserializeObject<T>(val.ToString());
            MessageReceived?.Invoke(obj);
        }
    }
}