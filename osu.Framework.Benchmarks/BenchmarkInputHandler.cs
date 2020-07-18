// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkInputHandler : BenchmarkTest
    {
        private const int repetitions = 1000;

        private readonly TestInputHandler[] inputHandlers = new TestInputHandler[repetitions];

        public override void SetUp()
        {
            base.SetUp();

            for (var i = 0; i < repetitions; i++)
            {
                inputHandlers[i] = new TestInputHandler();

                for (var j = 0; j < repetitions; j++)
                {
                    inputHandlers[i].PendingInputs.Enqueue(new KeyboardKeyInput(Key.A, true));
                }
            }
        }

        [Benchmark]
        public void GetPendingInputs()
        {
            for (var i = 0; i < repetitions; i++)
            {
                // ReSharper disable once UnusedVariable
                foreach (var pendingInput in inputHandlers[i].GetPendingInputs())
                {
                }
            }
        }

        private class TestInputHandler : InputHandler
        {
            public override bool IsActive => throw new NotImplementedException();

            public override int Priority => throw new NotImplementedException();

            public override bool Initialize(GameHost host) => throw new NotImplementedException();

            internal new ConcurrentQueue<IInput> PendingInputs => base.PendingInputs;
        }
    }
}
