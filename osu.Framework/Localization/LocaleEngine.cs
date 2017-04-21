// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace osu.Framework.Localization
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

        protected void AddWeakReference(UnicodeBindableString unicodeBindable) => unicodeBindings.Add(new WeakReference<UnicodeBindableString>(unicodeBindable));

        public UnicodeBindableString GetUnicodePreference(string unicode, string nonUnicode)
        {
            var bindable = new UnicodeBindableString(unicode, nonUnicode)
            {
                PreferUnicode = preferUnicode.Value
            };
            AddWeakReference(bindable);

            return bindable;
        }

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
            string validLocale=null;
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
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(validLocale);
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
    }
}
