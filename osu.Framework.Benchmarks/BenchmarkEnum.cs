// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkEnum
    {
        [Benchmark]
        public bool HasFlag()
        {
            bool result = false;

#pragma warning disable RS0030 // (banned API)
            for (int i = 0; i < 1000000; i++)
                result |= getFlags(i).HasFlag((FlagsEnum)i);
#pragma warning restore RS0030

            return result;
        }

        [Benchmark]
        public bool BitwiseAnd()
        {
            bool result = false;

            for (int i = 0; i < 1000000; i++)
                result |= (getFlags(i) & (FlagsEnum)i) > 0;

            return result;
        }

        [Benchmark]
        public bool HasFlagFast()
        {
            bool result = false;

            for (int i = 0; i < 1000000; i++)
                result |= getFlags(i).HasFlagFast((FlagsEnum)i);

            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private FlagsEnum getFlags(int i) => (FlagsEnum)i;

        [Flags]
        private enum FlagsEnum
        {
            Flag1 = 1,
            Flag2 = 2,
            Flag3 = 4,
            Flag4 = 8
        }
    }
}
