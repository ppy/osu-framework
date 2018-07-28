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
    public class LocalisationEngine : ILocalisationEngine
    {
        private readonly Bindable<bool> preferUnicode;
        private readonly Bindable<string> locale;

        private readonly WeakList<LocalisedBindable> localisedBindings = new WeakList<LocalisedBindable>();
        private readonly WeakList<UnicodeBindable> unicodeBindings = new WeakList<UnicodeBindable>();

        private readonly Dictionary<string, IResourceStore<string>> storages = new Dictionary<string, IResourceStore<string>>();
        private IResourceStore<string> current;

        public virtual IEnumerable<string> SupportedLocales => storages.Keys;
        public IEnumerable<KeyValuePair<string, string>> SupportedLanguageNames => SupportedLocales.Select(x => new KeyValuePair<string, string>(x, new CultureInfo(x).NativeName));

        public LocalisationEngine(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkSetting.ShowUnicode);
            preferUnicode.ValueChanged += prefer =>
            {
                lock (unicodeBindings)
                    unicodeBindings.ForEachAlive(b => b.PreferUnicode = prefer);
            };

            locale = config.GetBindable<string>(FrameworkSetting.Locale);
            locale.ValueChanged += checkLocale;
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
            var bindable = new LocalisedBindable(localisable);

            if (localisable.Type != LocalisationType.Never)
            {
                lock (localisedBindings)
                    localisedBindings.Add(bindable);

                bindable.Localisable.Type.ValueChanged += _ => updateLocalisation(bindable);
                bindable.Localisable.Text.ValueChanged += _ => updateLocalisation(bindable);
                bindable.Localisable.Args.ValueChanged += _ => updateLocalisation(bindable);

                updateLocalisation(bindable);
            }

            return bindable;
        }

        private void updateLocalisation(LocalisedBindable bindable)
        {
            var localisable = bindable.Localisable;
            string newText = localisable.Text;

            if ((localisable.Type & LocalisationType.Localised) > 0)
                newText = GetLocalised(newText);

            if ((localisable.Type & LocalisationType.Formatted) > 0 && localisable.Args.Value != null && newText != null)
            {
                try
                {
                    newText = string.Format(newText, localisable.Args.Value);
                }
                catch (FormatException)
                {
                    // let's catch this here to prevent crashes. the string will be in its non-formatted state
                }
            }

            bindable.Value = newText;
        }

        /// <summary>
        /// Creates and tracks a <see cref="Bindable{T}"/> that is one of two given string values, based on the <see cref="FrameworkSetting.ShowUnicode"/>.
        /// </summary>
        /// <param name="unicode">The unicode text to be used when <see cref="FrameworkSetting.ShowUnicode"/> is true.</param>
        /// <param name="nonUnicode">The non-unicode text to be used when <see cref="FrameworkSetting.ShowUnicode"/> is false.</param>
        /// <returns>A <see cref="Bindable{T}"/> that contains either the unicode or non-unicode text and updates dynamically.</returns>
        [NotNull]
        public IBindable<string> GetUnicodeBindable([CanBeNull] string unicode, [CanBeNull] string nonUnicode)
        {
            var bindable = new UnicodeBindable(unicode, nonUnicode)
            {
                PreferUnicode = preferUnicode.Value
            };

            lock (unicodeBindings)
                unicodeBindings.Add(bindable);

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

                lock (localisedBindings)
                    localisedBindings.ForEachAlive(updateLocalisation);
            }
        }

        protected virtual void ChangeLocale(string locale) => current = storages[locale];
    }

    internal class LocalisedBindable : Bindable<string>
    {
        public LocalisableString Localisable { get; }

        public LocalisedBindable(LocalisableString localisable)
            : base(localisable.Text)
        {
            Localisable = localisable;
        }
    }

    internal class UnicodeBindable : Bindable<string>
    {
        private readonly string unicode, nonUnicode;

        public UnicodeBindable(string unicode, string nonUnicode)
            : base(nonUnicode)
        {
            this.unicode = unicode ?? nonUnicode;
            this.nonUnicode = nonUnicode ?? unicode;
        }

        public bool PreferUnicode
        {
            get => Value == unicode;
            set => Value = value ? unicode : nonUnicode;
        }
    }
}
