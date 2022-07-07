// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkButtonStates
    {
        [Benchmark(Baseline = true)]
        public ButtonStates<MouseButton> CreateNew() => new ButtonStates<MouseButton>();

        [Arguments(1)]
        [Arguments(5)]
        [Arguments(10)]
        [Arguments(50)]
        [Benchmark]
        public bool SetPressed()
        {
            var states = new ButtonStates<MouseButton>();
            states.SetPressed(MouseButton.Button1, true);
            return states.HasAnyButtonPressed;
        }

        [Benchmark]
        public int EnumerateEmptyDifferences() => new ButtonStates<MouseButton>().EnumerateDifference(new ButtonStates<MouseButton>()).Pressed.Length;

        [Arguments(1)]
        [Arguments(5)]
        [Arguments(10)]
        [Arguments(50)]
        [Benchmark]
        public int EnumerateBothDifferences(int count)
        {
            var state1 = new ButtonStates<Key>();
            var state2 = new ButtonStates<Key>();

            for (int i = 0; i < count; i++)
                state1.SetPressed((Key)i, true);

            for (int i = count; i < 2 * count; i++)
                state2.SetPressed((Key)i, true);

            return state1.EnumerateDifference(state2).Pressed.Length;
        }
    }
}
