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
        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> locale;

        private readonly Dictionary<string, IResourceStore<string>> storages = new Dictionary<string, IResourceStore<string>>();
        private IResourceStore<string> current;

        public virtual IEnumerable<string> SupportedLocales => storages.Keys;

        public LocalisationEngine(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkSetting.ShowUnicode);

            locale = config.GetBindable<string>(FrameworkSetting.Locale);
            locale.BindValueChanged(updateLocale, true);
        }

        public void AddLanguage(string language, IResourceStore<string> storage)
        {
            storages.Add(language, storage);
            locale.TriggerChange();
        }

        /// <summary>
        /// Creates and tracks a <see cref="Bindable{T}"/> according to information provided in <paramref name="localisable"/>, with the ability to dynamically update the bindable.
        /// </summary>
        /// <param name="localisable">Provides information about the text and expected type of localisation.</param>
        /// <returns>A <see cref="Bindable{T}"/> that contains the localised text as specified by the input <paramref name="localisable"/>.</returns>
        [NotNull]
        public IBindable<string> GetLocalisedBindable([NotNull] LocalisableString localisable)
        {
            var bindable = new LocalisedBindable(localisable, this);
            bindable.Locale.BindTo(locale);

            return bindable;
        }

        /// <summary>
        /// Creates and tracks a <see cref="Bindable{T}"/> that is one of two given string values, based on the value of <see cref="FrameworkSetting.ShowUnicode"/>.
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
            var locales = SupportedLocales.ToList();
            string validLocale = null;

            if (locales.Contains(newValue))
                validLocale = newValue;
            else
            {
                var culture = string.IsNullOrEmpty(newValue) ? CultureInfo.CurrentCulture : new CultureInfo(newValue);

                for (var c = culture; !c.Equals(CultureInfo.InvariantCulture); c = c.Parent)
                {
                    if (locales.Contains(c.Name))
                    {
                        validLocale = c.Name;
                        break;
                    }
                }

                if (validLocale == null)
                    validLocale = locales[0];
            }

            if (validLocale != newValue)
                locale.Value = validLocale;
            else
            {
                var culture = new CultureInfo(validLocale);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                current = storages[locale];
            }
        }

        private string getLocalised(string key) => current.Get(key);


    }
}
