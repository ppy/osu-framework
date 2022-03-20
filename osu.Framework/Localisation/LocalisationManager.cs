// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Configuration;

#nullable enable

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        public IBindable<LocalisationParameters> CurrentParameters => currentParameters;

        private readonly Bindable<LocalisationParameters> currentParameters = new Bindable<LocalisationParameters>(new LocalisationParameters(null, false));

        private readonly List<LocaleMapping> locales = new List<LocaleMapping>();

        private readonly Bindable<string> configLocale = new Bindable<string>();
        private readonly Bindable<bool> configPreferUnicode = new BindableBool();

        public LocalisationManager(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.Locale, configLocale);
            configLocale.BindValueChanged(updateLocale);

            config.BindWith(FrameworkSetting.ShowUnicode, configPreferUnicode);
            configPreferUnicode.BindValueChanged(updateUnicodePreference, true);
        }

        public void AddLanguage(string language, ILocalisationStore storage)
        {
            locales.Add(new LocaleMapping(language, storage));
            configLocale.TriggerChange();
        }

        /// <summary>
        /// Returns the appropriate <see cref="string"/> value for a <see cref="LocalisableString"/> given the currently valid <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <remarks>
        /// The returned value is only valid until the next change to <see cref="CurrentParameters"/>.
        /// To facilitate tracking changes to the localised value across <see cref="CurrentParameters"/> changes, use <see cref="GetLocalisedBindableString"/>
        /// and subscribe to its <see cref="Bindable{T}.ValueChanged"/> instead.
        /// </remarks>
        internal string GetLocalisedString(LocalisableString text)
        {
            switch (text.Data)
            {
                case string plain:
                    return plain;

                case ILocalisableStringData data:
                    return data.GetLocalised(currentParameters.Value);

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Creates an <see cref="ILocalisedBindableString"/> which automatically updates its text according to information provided in <see cref="ILocalisedBindableString.Text"/>.
        /// </summary>
        /// <returns>The <see cref="ILocalisedBindableString"/>.</returns>
        public ILocalisedBindableString GetLocalisedBindableString(LocalisableString original) => new LocalisedBindableString(original, this);

        private void updateLocale(ValueChangedEvent<string> locale)
        {
            if (locales.Count == 0)
                return;

            var validLocale = locales.Find(l => l.Name == locale.NewValue);

            if (validLocale == null)
            {
                var culture = string.IsNullOrEmpty(locale.NewValue) ? CultureInfo.CurrentCulture : new CultureInfo(locale.NewValue);

                for (var c = culture; !EqualityComparer<CultureInfo>.Default.Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
                {
                    validLocale = locales.Find(l => l.Name == c.Name);
                    if (validLocale != null)
                        break;
                }

                validLocale ??= locales[0];
            }

            ChangeSettings(CreateNewLocalisationParameters(validLocale.Storage, currentParameters.Value.PreferOriginalScript));
        }

        private void updateUnicodePreference(ValueChangedEvent<bool> preferUnicode)
            => ChangeSettings(CreateNewLocalisationParameters(currentParameters.Value.Store, preferUnicode.NewValue));

        /// <summary>
        /// Changes the localisation parameters.
        /// </summary>
        /// <param name="parameters">The new localisation parameters.</param>
        protected void ChangeSettings(LocalisationParameters parameters) => currentParameters.Value = parameters;

        /// <summary>
        /// Creates new <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <remarks>
        /// Can be overridden to provide custom parameters for <see cref="ILocalisableStringData"/> implementations.
        /// </remarks>
        /// <param name="store">The <see cref="ILocalisationStore"/> to be used for string lookups and culture-specific formatting.</param>
        /// <param name="preferOriginalScript">Whether to prefer the "original" script of <see cref="RomanisableString"/>s.</param>
        /// <returns>The resultant <see cref="LocalisationParameters"/>.</returns>
        protected virtual LocalisationParameters CreateNewLocalisationParameters(ILocalisationStore? store, bool preferOriginalScript)
            => new LocalisationParameters(store, preferOriginalScript);

        private class LocaleMapping
        {
            public readonly string Name;
            public readonly ILocalisationStore Storage;

            public LocaleMapping(string name, ILocalisationStore storage)
            {
                Name = name;
                Storage = storage;
            }
        }
    }
}
