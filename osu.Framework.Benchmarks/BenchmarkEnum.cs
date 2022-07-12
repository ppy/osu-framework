// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
#pragma warning disable RS0030 // (banned API)
            return (FlagsEnum.Flag2 | FlagsEnum.Flag3).HasFlag(FlagsEnum.Flag2);
#pragma warning restore RS0030
        }

        [Benchmark]
        public bool BitwiseAnd()
        {
            return ((FlagsEnum.Flag2 | FlagsEnum.Flag3) & FlagsEnum.Flag2) > 0;
        }

        [Benchmark]
        public bool HasFlagFast()
        {
            return (FlagsEnum.Flag2 | FlagsEnum.Flag3).HasFlagFast(FlagsEnum.Flag2);
        }

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
