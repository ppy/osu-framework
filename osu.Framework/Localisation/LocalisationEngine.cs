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
        private readonly List<WeakReference<FormattableString>> formattableBindings = new List<WeakReference<FormattableString>>();

        protected void AddWeakReference(UnicodeBindableString unicodeBindable) => unicodeBindings.Add(new WeakReference<UnicodeBindableString>(unicodeBindable));
        protected void AddWeakReference(LocalisedString localisedBindable) => localisedBindings.Add(new WeakReference<LocalisedString>(localisedBindable));
        protected void AddWeakReference(FormattableString formattableBinding) => formattableBindings.Add(new WeakReference<FormattableString>(formattableBinding));

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

        public FormattableString Format(string format, params object[] objects)
        {
            var bindable = new FormattableString(format, objects);
            AddWeakReference(bindable);

            return bindable;
        }

        public VaraintFormattableString FormatVariant(string formatKey, params object[] objects)
        {
            var bindable = new VaraintFormattableString(GetLocalisedString(formatKey), objects);
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
                updateFormattableString();
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

        private void updateFormattableString()
        {
            foreach (var w in formattableBindings.ToArray())
            {
                FormattableString b;
                if (w.TryGetTarget(out b))
                    b.Update();
                else
                    formattableBindings.Remove(w);
            }
        }
    }
}
