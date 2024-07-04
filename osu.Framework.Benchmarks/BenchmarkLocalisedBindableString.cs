// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Configuration;
using osu.Framework.Localisation;
using osu.Framework.Testing;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkLocalisedBindableString
    {
        private LocalisationManager manager = null!;
        private TemporaryNativeStorage storage = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            storage = new TemporaryNativeStorage(Guid.NewGuid().ToString());
            manager = new LocalisationManager(new FrameworkConfigManager(storage));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            manager.Dispose();
            storage.Dispose();
        }

        [Benchmark]
        public void BenchmarkNonLocalised()
        {
            var bindable = manager.GetLocalisedBindableString("test");
            bindable.UnbindAll();
        }

        [Benchmark]
        public void BenchmarkLocalised()
        {
            var bindable = manager.GetLocalisedBindableString(new TranslatableString("test", "test"));
            bindable.UnbindAll();
        }
    }
}
