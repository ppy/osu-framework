// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public class LocalisationEngine
    {
        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> locale;

        public virtual IEnumerable<string> SupportedLocales => new[] { "en" };
        public IEnumerable<string> SupportedLanguageNames => SupportedLocales.Select(x => new CultureInfo(x).NativeName);

        public LocalisationEngine(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkConfig.ShowUnicode);
            preferUnicode.ValueChanged += updateUnicodeStrings;

            locale = config.GetBindable<string>(FrameworkConfig.Locale);
            locale.ValueChanged += checkLocale;
            locale.TriggerChange();
        }

        private readonly List<WeakReference<UnicodeBindableString>> unicodeBindings = new List<WeakReference<UnicodeBindableString>>();
        private readonly List<WeakReference<LocalisedString>> localisedBindings = new List<WeakReference<LocalisedString>>();

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

        protected virtual string GetLocalised(string key) => $"{key} in {CultureInfo.CurrentCulture.NativeName}";

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
            var locales = SupportedLocales.ToList();
            string validLocale = null;

            if (locales.Contains(newValue))
                validLocale = newValue;
            else
            {
                var culture = string.IsNullOrEmpty(newValue) ?
                    CultureInfo.CurrentCulture :
                    new CultureInfo(newValue);

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
                updateLocalisedString();
            }
        }

        protected virtual void ChangeLocale(string locale) => Logging.Logger.Log($"locale changed to {locale}");

        private void updateLocalisedString()
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

        /// <summary>
        /// A Bindable string which takes a unicode and non-unicode (usually romanised) version of the contained text
        /// and provides automatic switching behaviour should the user change their preference.
        /// </summary>
        public class UnicodeBindableString : Bindable<string>
        {
            public readonly string Unicode;
            public readonly string NonUnicode;

            public UnicodeBindableString(string unicode, string nonUnicode) : base(nonUnicode)
            {
                Unicode = unicode;
                NonUnicode = nonUnicode;

                if (Unicode == null)
                    Unicode = NonUnicode;
                if (NonUnicode == null)
                    NonUnicode = Unicode;
            }

            public bool PreferUnicode
            {
                get { return Value == Unicode; }
                set { Value = value ? Unicode : NonUnicode; }
            }
        }

        /// <summary>
        /// A Bindable string which stays up-to-date with the current locale choice for the specified key.
        /// </summary>
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
