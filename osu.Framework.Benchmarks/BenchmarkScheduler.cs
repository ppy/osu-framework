// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Threading;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkScheduler : BenchmarkTest
    {
        private Scheduler scheduler = null!;
        private Scheduler schedulerWithEveryUpdate = null!;
        private Scheduler schedulerWithManyDelayed = null!;

        public override void SetUp()
        {
            base.SetUp();

            scheduler = new Scheduler();

            schedulerWithEveryUpdate = new Scheduler();
            schedulerWithEveryUpdate.AddDelayed(() => { }, 0, true);

            schedulerWithManyDelayed = new Scheduler();
            for (int i = 0; i < 1000; i++)
                schedulerWithManyDelayed.AddDelayed(() => { }, int.MaxValue);
        }

        [Benchmark]
        public void UpdateEmptyScheduler()
        {
            for (int i = 0; i < 1000; i++)
                scheduler.Update();
        }

        [Benchmark]
        public void UpdateSchedulerWithManyDelayed()
        {
            for (int i = 0; i < 1000; i++)
                schedulerWithManyDelayed.Update();
        }

        [Benchmark]
        public void UpdateSchedulerWithEveryUpdate()
        {
            for (int i = 0; i < 1000; i++)
                schedulerWithEveryUpdate.Update();
        }

        [Benchmark]
        public void UpdateSchedulerWithManyAdded()
        {
            for (int i = 0; i < 1000; i++)
            {
                scheduler.Add(() => { });
                scheduler.Update();
            }
        }
    }
}
