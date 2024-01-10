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
            Queue<GazePointData> dataQueue = new Queue<GazePointData>();
            int queueSize = 7;
            bool isNoPositionPeriod = false;
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


                if (decodedData != null && decodedData.Valid && lastTimestamp != 0 && decodedData.TimestampNum - lastTimestamp > 500)
                {
                    //if (!isNoPositionPeriod)
                    //{
                    // if queue is full, we invoke NoPositionPeriodEnded and dequeue and set isNoPositionPeriod to false
                    // i.e. click
                    //if (dataQueue.Count == queueSize) // >=?
                    //{
                    //    // remove all elements from queue
                    //    while (dataQueue.Count > 0)
                    //    { dataQueue.Dequeue(); }

                    //    // end of click
                    //    if (isNoPositionPeriod)
                    //    {
                    //        NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                    //        isNoPositionPeriod = false;
                    //    }
                    //    else
                    //    {
                    //        NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                    //        isNoPositionPeriod = true;
                    //    }

                    //}
                    //dataQueue.Enqueue(decodedData);
                    // end of click
                    NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                    NoPositionPeriodEnded?.Invoke(MouseButton.Left);

                    //if (!isNoPositionPeriod)
                    //{
                    //    NoPositionPeriodStarted?.Invoke(MouseButton.Left);
                    //    isNoPositionPeriod = true;

                    //}
                    //else
                    //{
                    //    NoPositionPeriodEnded?.Invoke(MouseButton.Left);
                    //    isNoPositionPeriod = false;
                    //}

                        //dataQueue.Enqueue(decodedData);


                    //}

                    //fout.Write(Encoding.ASCII.GetBytes("Clicking and skipping invalid data.\n"));
                    lastTimestamp = decodedData.TimestampNum;
                    //var position2 = new Vector2(decodedData.X * bounds.Width + bounds.Left, decodedData.Y * bounds.Height + bounds.Top);
                    //fout.Write(Encoding.ASCII.GetBytes($"Halt Position: {position2}\n"));
                    //AbsolutePositionChanged?.Invoke(position2);
                    //return;
                }

                lastTimestamp = decodedData.TimestampNum;

                var position = new Vector2(decodedData.X * bounds.Width + bounds.Left, decodedData.Y * bounds.Height + bounds.Top);
                fout.Write(Encoding.ASCII.GetBytes($"Position: {position}\n"));
                AbsolutePositionChanged?.Invoke(position);
            }
        }
    }
}
