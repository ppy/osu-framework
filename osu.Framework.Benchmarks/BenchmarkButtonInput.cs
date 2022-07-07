// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkButtonInput
    {
        [Arguments(1)]
        [Arguments(5)]
        [Arguments(10)]
        [Arguments(50)]
        [Benchmark]
        public KeyboardKeyInput FromTwoStates(int count)
        {
            var state1 = new ButtonStates<Key>();
            var state2 = new ButtonStates<Key>();

            for (int i = 0; i < count; i++)
                state1.SetPressed((Key)i, true);

            for (int i = count; i < 2 * count; i++)
                state2.SetPressed((Key)i, true);

            return new KeyboardKeyInput(state1, state2);
        }

        [Benchmark]
        public InputState Apply()
        {
            var entries = new List<ButtonInputEntry<Key>>();

            for (int i = 0; i < 10; i++)
                entries.Add(new ButtonInputEntry<Key>((Key)i, true));

            var input = new KeyboardKeyInput(entries);
            var state = new InputState();
            input.Apply(state, new NullStateChangeHandler());

            return state;
        }

        private struct NullStateChangeHandler : IInputStateChangeHandler
        {
            public void HandleInputStateChange(InputStateChangeEvent inputStateChange)
            {
            }
        }
    }
}
