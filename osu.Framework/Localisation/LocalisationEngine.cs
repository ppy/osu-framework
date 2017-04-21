// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace osu.Framework.Localisation
{
    public class LocalisationEngine
    {
        private Bindable<bool> preferUnicode;
        private Bindable<string> locale;

        public virtual string[] SupportedLocales => new[] { "en" };

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkConfig.ShowUnicode);
            preferUnicode.ValueChanged += updateUnicodeStrings;
            locale = config.GetBindable<string>(FrameworkConfig.Locale);
            locale.ValueChanged += checkLocale;
            locale.TriggerChange();
        }

        private List<WeakReference<UnicodeBindableString>> unicodeBindings = new List<WeakReference<UnicodeBindableString>>();
        private List<WeakReference<LocalisedString>> localisedBindings = new List<WeakReference<LocalisedString>>();

        protected void AddWeakReference(UnicodeBindableString unicodeBindable) => unicodeBindings.Add(new WeakReference<UnicodeBindableString>(unicodeBindable));
        protected void AddWeakReference(LocalisedString localisedBindable) => localisedBindings.Add(new WeakReference<LocalisedString>(localisedBindable));

        public UnicodeBindableString GetUnicodePreference(string unicode, string nonUnicode)
        {
            var bindable = new UnicodeBindableString(unicode, nonUnicode)
            {
                PreferUnicode = preferUnicode.Value
            };
            AddWeakReference(bindable);

            return bindable;
        }

        public LocalisedString GetLocalisedString(string key)
        {
            var bindable = new LocalisedString(key)
            {
                Value = GetLocalised(key)
            };
            AddWeakReference(bindable);

            return bindable;
        }

        protected virtual string GetLocalised(string key) => $"{key} in {CultureInfo.DefaultThreadCurrentCulture.DisplayName}";

        private void updateUnicodeStrings(bool newValue)
        {
            foreach (var w in unicodeBindings.ToArray())
            {
                UnicodeBindableString b;
                if (w.TryGetTarget(out b))
                    b.PreferUnicode = newValue;
                else
                    unicodeBindings.Remove(w);
            }
        }

        private void checkLocale(string newValue)
        {
            CultureInfo culture;
            if (newValue == null) //means use current locale
                culture = CultureInfo.CurrentCulture;
            else
                culture = new CultureInfo(newValue);

            var locales = SupportedLocales.ToList();
            string validLocale = null;
            for (var c = culture; !c.IsNeutralCulture; c = c.Parent)
                if (locales.Contains(c.Name))
                {
                    validLocale = c.Name;
                    break;
                }

            if (validLocale == null)
                validLocale = locales[0];
            if (validLocale != newValue)
                locale.Value = validLocale;
            else
            {
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(validLocale);
                ChangeLocale(validLocale);
                updateLocalisedString(validLocale);
            }
        }

        protected virtual void ChangeLocale(string locale) { }

        private void updateLocalisedString(string culture)
        {
            foreach (var w in localisedBindings.ToArray())
            {
                LocalisedString b;
                if (w.TryGetTarget(out b))
                    b.Value = GetLocalised(b.Key);
                else
                    localisedBindings.Remove(w);
            }
        }

        public class UnicodeBindableString : Bindable<string>
        {
            public readonly string Unicode;
            public readonly string NonUnicode;

            public UnicodeBindableString(string unicode, string nonUnicode) : base(nonUnicode)
            {
                Unicode = unicode;
                NonUnicode = nonUnicode;
            }

            public bool PreferUnicode
            {
                get { return Value == Unicode; }
                set { Value = value ? Unicode : NonUnicode; }
            }
        }

        public class LocalisedString : Bindable<string>
        {
            public readonly string Key;
            public LocalisedString(string key)
            {
                Key = key;
            }
        }
    }
}
