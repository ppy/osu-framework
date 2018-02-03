// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;
using osu.Framework.Lists;
using JetBrains.Annotations;

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
            preferUnicode.ValueChanged += _ =>
            {
                lock (allBindings)
                    allBindings.ForEachAlive(updateLocalisation);
            };

            locale = config.GetBindable<string>(FrameworkSetting.Locale);
            locale.ValueChanged += checkLocale;
        }

        private readonly WeakList<LocalisedBindable> allBindings = new WeakList<LocalisedBindable>();

        public void AddLanguage(string language, IResourceStore<string> storage)
        {
            storages.Add(language, storage);
            locale.TriggerChange();
        }

        /// <summary>
        /// Get a localised <see cref="Bindable{String}"/> for a <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="localisable">The <see cref="LocalisableString"/> to get a bindable for. 
        /// <para>Changing any of its bindables' values will also trigger a localisation update.</para></param>
        [NotNull]
        public Bindable<string> GetBindableFor([NotNull] LocalisableString localisable)
        {
            var bindable = new LocalisedBindable(localisable);
            lock (allBindings)
                allBindings.Add(bindable);

            bindable.Localisable.Type.ValueChanged += _ => updateLocalisation(bindable);
            bindable.Localisable.Text.ValueChanged += _ => updateLocalisation(bindable);
            bindable.Localisable.NonUnicode.ValueChanged += _ => updateLocalisation(bindable);
            bindable.Localisable.Args.ValueChanged += _ => updateLocalisation(bindable);

            updateLocalisation(bindable);

            return bindable;
        }

        /// <summary>
        /// Updates the localisation for a specific <see cref="LocalisedBindable"/>, optionally only for specific <see cref="LocalisationType"/>s.
        /// </summary>
        /// <param name="bindable">The bindable to update.</param>
        private void updateLocalisation(LocalisedBindable bindable)
        {
            var localisable = bindable.Localisable;
            string newText = localisable.Text.Value;

            if ((localisable.Type & LocalisationType.UnicodePreference) > 0 && !preferUnicode.Value && localisable.NonUnicode != null)
                newText = localisable.NonUnicode;

            if ((localisable.Type & LocalisationType.Localised) > 0)
                newText = GetLocalised(newText);

            if ((localisable.Type & LocalisationType.Formatted) > 0)
                newText = string.Format(newText, localisable.Args);

            bindable.Value = newText;
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

                lock (allBindings)
                    allBindings.ForEachAlive(updateLocalisation);
            }
        }

        protected virtual void ChangeLocale(string locale) => current = storages[locale];
    }

    internal class LocalisedBindable : Bindable<string>
    {
        private readonly WeakReference<LocalisableString> localisable;
        public LocalisableString Localisable => localisable.TryGetTarget(out var t) ? t : throw new ObjectDisposedException(nameof(localisable));

        public LocalisedBindable(LocalisableString localisable)
            : base(localisable.Text)
        {
            this.localisable = new WeakReference<LocalisableString>(localisable);
        }
    }
}
