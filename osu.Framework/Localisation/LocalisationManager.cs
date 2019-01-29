// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using JetBrains.Annotations;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private readonly List<LocaleMapping> locales = new List<LocaleMapping>();

        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> configLocale;
        private readonly Bindable<IResourceStore<string>> currentStorage = new Bindable<IResourceStore<string>>();

        public LocalisationManager(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkSetting.ShowUnicode);

            configLocale = config.GetBindable<string>(FrameworkSetting.Locale);
            configLocale.BindValueChanged(updateLocale);
        }

        public void AddLanguage(string language, IResourceStore<string> storage)
        {
            locales.Add(new LocaleMapping { Name = language, Storage = storage });
            configLocale.TriggerChange();
        }

        /// <summary>
        /// Creates an <see cref="ILocalisedBindableString"/> which automatically updates its text according to information provided in <see cref="ILocalisedBindableString.Text"/>.
        /// </summary>
        /// <returns>The <see cref="ILocalisedBindableString"/>.</returns>
        [NotNull]
        public ILocalisedBindableString GetLocalisedString(LocalisedString original) => new LocalisedBindableString(original, currentStorage, preferUnicode);

        private void updateLocale(string newValue)
        {
            if (locales.Count == 0)
                return;

            var validLocale = locales.Find(l => l.Name == newValue);

            if (validLocale == null)
            {
                var culture = string.IsNullOrEmpty(newValue) ? CultureInfo.CurrentCulture : new CultureInfo(newValue);

                for (var c = culture; !c.Equals(CultureInfo.InvariantCulture); c = c.Parent)
                {
                    validLocale = locales.Find(l => l.Name == c.Name);
                    if (validLocale != null)
                        break;
                }

                if (validLocale == null)
                    validLocale = locales[0];
            }

            if (validLocale.Name != newValue)
                configLocale.Value = validLocale.Name;
            else
            {
                var culture = new CultureInfo(validLocale.Name);

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                currentStorage.Value = validLocale.Storage;
            }
        }

        private class LocaleMapping
        {
            public string Name;
            public IResourceStore<string> Storage;
        }
    }
}
