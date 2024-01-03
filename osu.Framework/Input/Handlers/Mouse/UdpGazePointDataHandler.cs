using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    [PublicAPI]
    public class UdpGazePointDataHandler
    {
        public static UdpGazePointDataHandler Instance = new UdpGazePointDataHandler(new IPEndPoint(IPAddress.Loopback, 8052));

        public event Action<Vector2>? AbsolutePositionChanged;

        private readonly UdpClient server;

        private int lastTimestamp;
        private Rectangle bounds;

        private FileStream fout;

        public UdpGazePointDataHandler(IPEndPoint endpoint)
        {
            server = new UdpClient(endpoint);
            fout = new FileStream("./osu-eye-tracker-debug.log", FileMode.OpenOrCreate);
        }

        public void Initialize(GameHost gameHost)
        {
            bounds = gameHost.Window.PrimaryDisplay.Bounds;
            fout.Write(Encoding.ASCII.GetBytes($"Set up bounds: {bounds}\n"));
        }

        public void Receive()
        {
            while (true)
            {
                var sender = new IPEndPoint(IPAddress.Any, 0);
                var data = server.Receive(ref sender);
                var stringData = Encoding.ASCII.GetString(data);
                Console.WriteLine($"Received from {sender}: {stringData}");
                var decodedData = JsonConvert.DeserializeObject<GazePointData>(stringData);

                if (decodedData == null || !decodedData.Valid)
                {
                    fout.Write(Encoding.ASCII.GetBytes("Skipping invalid data.\n"));
                    return;
                }

                if (decodedData.TimestampNum < lastTimestamp)
                {
                    fout.Write(Encoding.ASCII.GetBytes($"Skipping too old data: {decodedData.TimestampNum} < {lastTimestamp}.\n"));
                    return;
                }

                lastTimestamp = decodedData.TimestampNum;
                var position = new Vector2(decodedData.X * bounds.Width + bounds.Left, decodedData.Y * bounds.Height + bounds.Top);
                fout.Write(Encoding.ASCII.GetBytes($"Position: {position}\n"));
                AbsolutePositionChanged?.Invoke(position);
            }
        }
    }
}
