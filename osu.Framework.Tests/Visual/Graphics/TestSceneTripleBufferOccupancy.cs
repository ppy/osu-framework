// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneTripleBufferOccupancy : FrameworkTestScene
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private readonly TextFlowContainer text;

        private long[] writes = new long[3];
        private long[] reads = new long[3];
        private Stopwatch stopwatch = Stopwatch.StartNew();

        private int writeLag;
        private int readLag;

        public TestSceneTripleBufferOccupancy()
        {
            Add(text = new TextFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            TripleBuffer<object> tripleBuffer = new TripleBuffer<object>();

            new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    using (var write = tripleBuffer.GetForWrite())
                        writes[write.Index]++;

                    if (writeLag != 0)
                        Thread.Sleep(writeLag);
                }
            }).Start();

            new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    using (var read = tripleBuffer.GetForRead())
                    {
                        if (read != null)
                            reads[read.Index]++;
                    }

                    if (readLag != 0)
                        Thread.Sleep(readLag);
                }
            }).Start();

            AddSliderStep("write lag", 0, 16, 0, v =>
            {
                writeLag = v;
                reset();
            });

            AddSliderStep("read lag", 0, 16, 0, v =>
            {
                readLag = v;
                reset();
            });

            reset();
        }

        private void reset()
        {
            writes = new long[3];
            reads = new long[3];
            stopwatch = Stopwatch.StartNew();
        }

        protected override void Update()
        {
            base.Update();

            StringBuilder info = new StringBuilder();

            double totalWrites = writes.Sum();
            double totalReads = reads.Sum();

            info.AppendLine("write occupancy:");
            for (int i = 0; i < writes.Length; i++)
                info.AppendLine($"{i}:   {writes[i] / totalWrites,-10:P}({writes[i]} / {totalWrites})");

            info.AppendLine();

            info.AppendLine("read occupancy:");
            for (int i = 0; i < reads.Length; i++)
                info.AppendLine($"{i}:   {reads[i] / totalReads,-10:P}({reads[i]} / {totalReads})");

            info.AppendLine();

            info.AppendLine($"Speed: {stopwatch.Elapsed.TotalMicroseconds / totalReads:0.00}us/read");

            text.Text = info.ToString();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            cts.Cancel();
        }
    }
}
