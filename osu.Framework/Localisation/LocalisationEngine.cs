// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;

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
            preferUnicode = config.GetBindable<bool>(FrameworkConfig.ShowUnicode);
            preferUnicode.ValueChanged += updateUnicodeStrings;

            locale = config.GetBindable<string>(FrameworkConfig.Locale);
            locale.ValueChanged += checkLocale;
        }

        private readonly List<WeakReference<UnicodeBindableString>> unicodeBindings = new List<WeakReference<UnicodeBindableString>>();
        private readonly List<WeakReference<LocalisedString>> localisedBindings = new List<WeakReference<LocalisedString>>();

        protected void AddWeakReference(UnicodeBindableString unicodeBindable) => unicodeBindings.Add(new WeakReference<UnicodeBindableString>(unicodeBindable));
        protected void AddWeakReference(LocalisedString localisedBindable) => localisedBindings.Add(new WeakReference<LocalisedString>(localisedBindable));

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

        protected virtual string GetLocalised(string key) => current.Get(key);

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

        protected virtual void ChangeLocale(string locale) => current = storages[locale];

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

        /// <summary>
        /// A bindable string constructed from <see cref="string.Format(string, object[])"/>.
        /// </summary>
        public class FormattableString : Bindable<string>
        {
            private string format;
            private readonly object[] objects;
            protected virtual string Format => format;
            public void Update() => Value = string.Format(Format, objects);

            public FormattableString(string format, object[] objects)
            {
                this.format = format;
                this.objects = objects;
            }
        }

        /// <summary>
        /// A bindable string constructed from <see cref="string.Format(string, object[])"/>, and <see cref="VaraintFormattableString.Format"/> changable.
        /// </summary>
        public class VaraintFormattableString : FormattableString
        {
            private readonly Bindable<string> formatSource;
            protected override string Format => formatSource.Value;

            public VaraintFormattableString(Bindable<string> formatSource, object[] objects)
                : base(null, objects)
            {
                this.formatSource = formatSource;
            }
        }
    }
}
