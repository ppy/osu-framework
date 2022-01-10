// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Logging;
using osu.Framework.Testing;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkLogger
    {
        private const int line_count = 10000;

        [GlobalSetup]
        public void GlobalSetup()
        {
            Logger.Storage = new TemporaryNativeStorage(Guid.NewGuid().ToString());
        }

        [Benchmark]
        public void LogManyLinesDebug()
        {
            for (int i = 0; i < line_count; i++)
            {
                Logger.Log("This is a test log line.", level: LogLevel.Debug);
            }

            Logger.Flush();
        }

        [Benchmark]
        public void LogManyMultiLineLines()
        {
            for (int i = 0; i < line_count; i++)
            {
                Logger.Log("This\nis\na\ntest\nlog\nline.");
            }

            Logger.Flush();
        }

        [Benchmark]
        public void LogManyLines()
        {
            for (int i = 0; i < line_count; i++)
            {
                Logger.Log("This is a test log line.");
            }

            Logger.Flush();
        }
    }
}
