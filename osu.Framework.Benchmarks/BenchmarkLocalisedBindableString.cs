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
        private LocalisationManager manager;
        private TemporaryNativeStorage storage;

        [GlobalSetup]
        public void GlobalSetup()
        {
            storage = new TemporaryNativeStorage(new Guid().ToString());
            manager = new LocalisationManager(new FrameworkConfigManager(storage));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
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
