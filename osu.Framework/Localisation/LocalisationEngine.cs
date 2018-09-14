// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using JetBrains.Annotations;

namespace osu.Framework.Localisation
{
    public partial class LocalisationEngine : ILocalisationEngine
    {
        private readonly List<LocaleMapping> locales = new List<LocaleMapping>();

        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> configLocale;
        private readonly Bindable<IResourceStore<string>> currentStorage = new Bindable<IResourceStore<string>>();

        public LocalisationEngine(FrameworkConfigManager config)
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
        /// Creates a <see cref="Bindable{T}"/> which automatically updates its text according to information provided in <paramref name="localisable"/>.
        /// </summary>
        /// <param name="localisable">Provides information about the text and expected type of localisation.</param>
        /// <returns>A <see cref="Bindable{T}"/> that contains the localised text as specified by the input <paramref name="localisable"/>.</returns>
        [NotNull]
        public IBindable<string> GetLocalisedBindable([NotNull] LocalisableString localisable)
        {
            var bindable = new LocalisedBindable(localisable);
            bindable.Storage.BindTo(currentStorage);

            return bindable;
        }

        /// <summary>
        /// Creates a <see cref="Bindable{T}"/> which automatically switches its text according to <see cref="FrameworkSetting.ShowUnicode"/>.
        /// </summary>
        /// <param name="unicode">The unicode text to be used when <see cref="FrameworkSetting.ShowUnicode"/> is true.</param>
        /// <param name="nonUnicode">The non-unicode text to be used when <see cref="FrameworkSetting.ShowUnicode"/> is false.</param>
        /// <returns>A <see cref="Bindable{T}"/> that contains either the unicode or non-unicode text and updates dynamically.</returns>
        [NotNull]
        public IBindable<string> GetUnicodeBindable([CanBeNull] string unicode, [CanBeNull] string nonUnicode)
        {
            var bindable = new UnicodeBindable(unicode, nonUnicode);
            bindable.PreferUnicode.BindTo(preferUnicode);

            return bindable;
        }

        private void updateLocale(string newValue)
        {
            if (locales.Count == 0)
                return;

            var validLocale = locales.FirstOrDefault(l => l.Name == newValue);

            if (validLocale == null)
            {
                var culture = string.IsNullOrEmpty(newValue) ? CultureInfo.CurrentCulture : new CultureInfo(newValue);

                for (var c = culture; !c.Equals(CultureInfo.InvariantCulture); c = c.Parent)
                {
                    validLocale = locales.FirstOrDefault(l => l.Name == c.Name);
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
