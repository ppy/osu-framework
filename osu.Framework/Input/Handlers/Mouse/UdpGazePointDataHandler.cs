using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    [PublicAPI]
    public class UdpGazePointDataHandler
    {
        public static UdpGazePointDataHandler Instance = new UdpGazePointDataHandler(new IPEndPoint(IPAddress.Loopback, 8052));

        public event Action<Vector2>? AbsolutePositionChanged;
        public event Action<MouseButton>? NoPositionPeriodStarted;
        public event Action<MouseButton>? NoPositionPeriodEnded;

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
            //Queue<GazePointData> dataQueue = new Queue<GazePointData>();
            //int queueSize = 7;

            const uint blink_time = 300; //ms
            const uint wait_fblink = 300;

            bool waiting_fblink = false;
            bool draging = false;
            int last_blink_timestamp = 0;

            int frames_after_blinking = 10;
            double old_post_coef = 0.8, new_pos_coef = 0.2;

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

                // if blink happened
                if (decodedData != null && decodedData.Valid && lastTimestamp != 0 && decodedData.TimestampNum - lastTimestamp > blink_time)
                {
                    if (draging)
                    {
                        NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                        draging = false;
                    }
                    else if (waiting_fblink)
                    {
                        // start click&drag
                        NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                        draging = true;
                    }
                    else
                    {
                        // click down
                        NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                        waiting_fblink = true;
                        last_blink_timestamp = decodedData.TimestampNum;
                    }
                    lastTimestamp = decodedData.TimestampNum;
                    //fout.Write(Encoding.ASCII.GetBytes($"Blink: {d 

                    //NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                    //lastTimestamp = decodedData.TimestampNum;

                }
                // blink did not happen 
                else if (lastTimestamp != 0 && waiting_fblink && decodedData.TimestampNum - last_blink_timestamp >= wait_fblink)
                {
                    NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                    waiting_fblink = false;
                    last_blink_timestamp = 0; // not necessary probably
                }

                lastTimestamp = decodedData.TimestampNum;

                var position = new Vector2(decodedData.X * bounds.Width + bounds.Left, decodedData.Y * bounds.Height + bounds.Top);
                fout.Write(Encoding.ASCII.GetBytes($"Position: {position}\n"));
                AbsolutePositionChanged?.Invoke(position);
            }
        }
    }
}
