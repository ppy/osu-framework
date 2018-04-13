// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using osu.Framework.Lists;

namespace osu.Framework.Localisation
{
    public class LocalisationEngine
    {
        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> locale;
        private readonly Dictionary<string, IResourceStore<string>> storages = new Dictionary<string, IResourceStore<string>>();
        private IResourceStore<string> current;

        public virtual IEnumerable<string> SupportedLocales => storages.Keys;
        public IEnumerable<KeyValuePair<string, string>> SupportedLanguageNames => SupportedLocales.Select(x => new KeyValuePair<string, string>(x, new CultureInfo(x).NativeName));

        public LocalisationEngine(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkSetting.ShowUnicode);
            preferUnicode.ValueChanged += newValue =>
            {
                lock (unicodeBindings)
                    unicodeBindings.ForEachAlive(b => b.PreferUnicode = newValue);
            };

            locale = config.GetBindable<string>(FrameworkSetting.Locale);
            locale.ValueChanged += checkLocale;
        }

        private readonly WeakList<UnicodeBindableString> unicodeBindings = new WeakList<UnicodeBindableString>();
        private readonly WeakList<LocalisedString> localisedBindings = new WeakList<LocalisedString>();
        private readonly WeakList<FormatString> formattableBindings = new WeakList<FormatString>();

        public void AddLanguage(string language, IResourceStore<string> storage)
        {
            storages.Add(language, storage);
            locale.TriggerChange();
        }

        public UnicodeBindableString GetUnicodePreference(string unicode, string nonUnicode)
        {
            var bindable = new UnicodeBindableString(unicode, nonUnicode)
            {
                PreferUnicode = preferUnicode.Value
            };

            lock (unicodeBindings)
                unicodeBindings.Add(bindable);

            return bindable;
        }

        public LocalisedString GetLocalisedString(string key)
        {
            var bindable = new LocalisedString(key)
            {
                Value = GetLocalised(key)
            };

            lock (localisedBindings)
                localisedBindings.Add(bindable);

            return bindable;
        }

        public FormatString Format(FormattableString formattable)
        {
            var bindable = new FormatString(formattable);

            lock (formattableBindings)
                formattableBindings.Add(bindable);

            return bindable;
        }

        public FormatString FormatVariant(string formatKey, params object[] objects)
        {
            var bindable = new FormatString(new LocalisedFormatString(GetLocalisedString(formatKey), objects));

            lock (formattableBindings)
                formattableBindings.Add(bindable);

            return bindable;
        }

        protected virtual string GetLocalised(string key) => current.Get(key);

        private void checkLocale(string newValue)
        {
            var locales = SupportedLocales.ToList();
            string validLocale = null;

            if (locales.Contains(newValue))
                validLocale = newValue;
            else
            {
                var culture = string.IsNullOrEmpty(newValue) ? CultureInfo.CurrentCulture : new CultureInfo(newValue);

                for (var c = culture; !c.Equals(CultureInfo.InvariantCulture); c = c.Parent)
                    if (locales.Contains(c.Name))
                    {
                        validLocale = c.Name;
                        break;
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
                ChangeLocale(validLocale);

                lock (localisedBindings) localisedBindings.ForEachAlive(b => b.Value = GetLocalised(b.Key));
                lock (formattableBindings) formattableBindings.ForEachAlive(b => b.Update());
            }
        }

        protected virtual void ChangeLocale(string locale) => current = storages[locale];
    }
}
