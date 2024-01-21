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

            const uint blink_time = 150; //ms
            const uint max_blink_time = 600; //ms
            const uint wait_fblink = 400;

            bool waiting_fblink = false;
            bool draging = false;
            int last_blink_timestamp = 0;

            // smoth filter
            // 10 - for menu is ok, but for gameplay it is too much
            const int frames_after_blinking = 16;
            const double old_post_coef_start = 0.8, new_pos_coef_start = 1 - old_post_coef_start;

            int frames_after_blinking_counter = 0;
            Vector2 old_position = new Vector2(0, 0);
            Vector2 pre_blink_position = new Vector2(0, 0);
            bool after_blink = false;

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

                if (lastTimestamp == 0) goto skip_filter;

                // if blink happened
                int dt = decodedData.TimestampNum - lastTimestamp;
                if (dt > blink_time && dt < max_blink_time)
                {
                    if (draging)
                    {
                        // second blink happened - start draging
                        NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                        draging = false;
                    }
                    else if (waiting_fblink)
                    {
                        // start click&drag
                        NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                        draging = true;
                        waiting_fblink = false;
                    }
                    else
                    {
                        // click down
                        NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                        waiting_fblink = true;
                        last_blink_timestamp = decodedData.TimestampNum;
                    }
                    after_blink = true;
                    pre_blink_position = old_position;
                    frames_after_blinking_counter = 0;

                }
                // blink did not happen 
                else if (waiting_fblink && decodedData.TimestampNum - last_blink_timestamp >= wait_fblink)
                {
                    NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                    waiting_fblink = false;
                    last_blink_timestamp = 0; // not necessary probably
                }

            skip_filter:
                lastTimestamp = decodedData.TimestampNum;

                if (after_blink && frames_after_blinking_counter < frames_after_blinking)
                {
                    fout.Write(Encoding.ASCII.GetBytes($"Waiting for blink: {decodedData.TimestampNum} - {last_blink_timestamp} >= {wait_fblink}.\n"));
                    var new_pos_coef = new_pos_coef_start + (1 - new_pos_coef_start) / frames_after_blinking;
                    var old_post_coef = old_post_coef_start - (old_post_coef_start) / frames_after_blinking;
                    old_position = new Vector2(
                                              (float)(old_post_coef * old_position.X + new_pos_coef * pre_blink_position.X),
                                              (float)(old_post_coef * old_position.Y + new_pos_coef * pre_blink_position.Y)
                                              );

                    frames_after_blinking_counter++;
                }
                else
                {
                    after_blink = false;
                    old_position = new Vector2(decodedData.X * bounds.Width + bounds.Left, decodedData.Y * bounds.Height + bounds.Top);
                }
                fout.Write(Encoding.ASCII.GetBytes($"Position_: {old_position}\n"));
                AbsolutePositionChanged?.Invoke(old_position);
            }
        }
    }
}
