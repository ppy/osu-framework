// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager : IDisposable
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
            configPreferUnicode.BindValueChanged(_ => UpdateLocalisationParameters(), true);
        }

        /// <summary>
        /// Add multiple locale mappings. Should be used to add all available languages at initialisation.
        /// </summary>
        /// <param name="mappings">All available locale mappings.</param>
        public void AddLocaleMappings(IEnumerable<LocaleMapping> mappings)
        {
            locales.AddRange(mappings);
            configLocale.TriggerChange();
        }

        /// <summary>
        /// Add a single language to this manager.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AddLocaleMappings"/> as a more efficient way of bootstrapping all available locales.</remarks>
        /// <param name="language">The culture name to be added. Generally should match <see cref="CultureInfo.Name"/>.</param>
        /// <param name="storage">A storage providing localisations for the specified language.</param>
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

        private LocaleMapping? currentLocale;

        private void updateLocale(ValueChangedEvent<string> locale)
        {
            if (locales.Count == 0)
                return;

            currentLocale = locales.Find(l => l.Name == locale.NewValue);

            if (currentLocale == null)
            {
                CultureInfo culture;

                if (string.IsNullOrEmpty(locale.NewValue))
                {
                    culture = CultureInfo.CurrentCulture;
                }
                else if (!CultureInfoHelper.TryGetCultureInfo(locale.NewValue, out culture))
                {
                    if (locale.OldValue == locale.NewValue)
                        // equal values mean invalid locale on startup, no real way to recover other than to set to default.
                        configLocale.SetDefault();
                    else
                        // revert to the old locale if the new one is invalid.
                        configLocale.Value = locale.OldValue;

                    return;
                }

                for (var c = culture; !EqualityComparer<CultureInfo>.Default.Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
                {
                    currentLocale = locales.Find(l => l.Name == c.Name);
                    if (currentLocale != null)
                        break;
                }

                currentLocale ??= locales[0];
            }

            UpdateLocalisationParameters();
        }

        /// <summary>
        /// Retrieves the latest localisation parameters using <see cref="CreateLocalisationParameters"/> and updates the current one with.
        /// </summary>
        protected void UpdateLocalisationParameters() => currentParameters.Value = CreateLocalisationParameters();

        /// <summary>
        /// Creates new <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <remarks>
        /// Can be overridden to provide custom parameters for <see cref="ILocalisableStringData"/> implementations.
        /// </remarks>
        /// <returns>The resultant <see cref="LocalisationParameters"/>.</returns>
        protected virtual LocalisationParameters CreateLocalisationParameters() => new LocalisationParameters(currentLocale?.Storage, configPreferUnicode.Value);

        protected virtual void Dispose(bool disposing)
        {
            currentParameters.UnbindAll();
            configLocale.UnbindAll();
            configPreferUnicode.UnbindAll();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
