// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Visual.Localisation
{
    public abstract partial class LocalisationTestScene : FrameworkTestScene
    {
        protected virtual LocalisationManager CreateLocalisationManager(FrameworkConfigManager config) => new LocalisationManager(config);

        protected LocalisationManager Manager = null!;

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.Locale"/>.
        /// </summary>
        protected Bindable<string> Locale = new Bindable<string>();

        /// <summary>
        /// Bound to <see cref="FrameworkSetting.ShowUnicode"/>.
        /// </summary>
        protected Bindable<bool> ShowUnicode { get; } = new BindableBool();

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(parent);

            var config = parent.Get<FrameworkConfigManager>();
            config.BindWith(FrameworkSetting.Locale, Locale);
            config.BindWith(FrameworkSetting.ShowUnicode, ShowUnicode);

            Locale.Value = string.Empty; // ensure a consistent initial language.

            dependencies.Cache(Manager = CreateLocalisationManager(config));

            return dependencies;
        }

        /// <summary>
        /// Adds a step that sets <see cref="Locale"/> to the specified value.
        /// </summary>
        protected void SetLocale(string locale)
        {
            AddStep($"change locale to '{locale}'", () => Locale.Value = locale);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (Manager.IsNotNull())
                Manager.Dispose();

            base.Dispose(isDisposing);
        }

        protected class TestLocalisationStore : ILocalisationStore
        {
            public CultureInfo EffectiveCulture { get; }

            private readonly IReadOnlyDictionary<string, string> translations;

            public TestLocalisationStore(CultureInfo culture, IReadOnlyDictionary<string, string> translations)
            {
                EffectiveCulture = culture;
                this.translations = translations;
            }

            public TestLocalisationStore(string locale, IReadOnlyDictionary<string, string> translations)
                : this(new CultureInfo(locale), translations)
            {
            }

            public string? Get(string key) => translations.TryGetValue(key, out string? value) ? value : null;

            public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Get(key));

            public Stream GetStream(string name) => throw new NotSupportedException();

            public IEnumerable<string> GetAvailableResources() => Array.Empty<string>();

            public void Dispose()
            {
            }
        }
    }
}
