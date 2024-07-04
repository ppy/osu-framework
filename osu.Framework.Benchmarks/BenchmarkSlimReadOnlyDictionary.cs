// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions.ListExtensions;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkSlimReadOnlyDictionary
    {
        private readonly Dictionary<int, int> dictionary = new Dictionary<int, int>();
        private ReadOnlyDictionary<int, int> readOnlyDictionary = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            readOnlyDictionary = new ReadOnlyDictionary<int, int>(dictionary);

            int[] values = { 0, 1, 2, 3, 4, 5, 3, 2, 3, 1, 4, 5, -1 };
            for (int i = 0; i < values.Length; i++)
                dictionary[i] = values[i];
        }

        [Benchmark]
        public int Dictionary()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach ((_, int v) in dictionary)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int DictionaryAsReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach ((_, int v) in readOnlyDictionary)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int DictionaryAsSlimReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach ((_, int v) in dictionary.AsSlimReadOnly())
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int Keys()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in dictionary.Keys)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int KeysAsReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in readOnlyDictionary.Keys)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int KeysAsSlimReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in dictionary.AsSlimReadOnly().Keys)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int Values()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in dictionary.Values)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int ValuesAsReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in readOnlyDictionary.Values)
                    sum += v;
            }

            return sum;
        }

        [Benchmark]
        public int ValuesAsSlimReadOnly()
        {
            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                foreach (int v in dictionary.AsSlimReadOnly().Values)
                    sum += v;
            }

            return sum;
        }
    }
}
