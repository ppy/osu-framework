// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager : IDisposable
    {
        public IBindable<LocalisationParameters> CurrentParameters => currentParameters;

        private readonly Bindable<LocalisationParameters> currentParameters = new Bindable<LocalisationParameters>(LocalisationParameters.DEFAULT);

        private readonly Dictionary<string, LocaleMapping> locales = new Dictionary<string, LocaleMapping>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The first locale added to <see cref="locales"/>.
        /// Used as the fallback locale if there are no matches.
        /// </summary>
        private LocaleMapping? firstLocale;

        private LocaleMapping? systemDefaultLocaleMapping;

        /// <summary>
        /// The <see cref="LocaleMapping"/> that most closely matches the <see cref="CultureInfoHelper.SystemUICulture"/>, or null iff <see cref="locales"/> is empty.
        /// </summary>
        /// <remarks>This property is cached.</remarks>
        public LocaleMapping? SystemDefaultLocaleMapping => systemDefaultLocaleMapping ??= getSystemDefaultLocaleMapping();

        private readonly Bindable<string> configLocale = new Bindable<string>();
        private readonly Bindable<bool> configPreferUnicode = new BindableBool();

        public LocalisationManager(FrameworkConfigManager config)
        {
            config.BindWith(FrameworkSetting.Locale, configLocale);
            configLocale.BindValueChanged(onLocaleChanged);

            config.BindWith(FrameworkSetting.ShowUnicode, configPreferUnicode);
            configPreferUnicode.BindValueChanged(preferUnicode =>
            {
                currentParameters.Value = currentParameters.Value.With(preferOriginalScript: preferUnicode.NewValue);
            }, true);
        }

        /// <summary>
        /// Add multiple locale mappings. Should be used to add all available languages at initialisation.
        /// </summary>
        /// <param name="mappings">All available locale mappings.</param>
        public void AddLocaleMappings(IEnumerable<LocaleMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                locales.Add(mapping.Name, mapping);
                firstLocale ??= mapping;
            }

            systemDefaultLocaleMapping = null; // invalidate stored default as there could be a better match.

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
            var mapping = new LocaleMapping(language, storage);

            locales.Add(language, mapping);
            firstLocale ??= mapping;

            systemDefaultLocaleMapping = null; // invalidate stored default as there could be a better match.

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

        private void onLocaleChanged(ValueChangedEvent<string> locale)
        {
            if (locales.Count == 0)
                return;

            if (tryGetLocaleMappingForLocale(locale.NewValue, out var localeMapping))
            {
                currentParameters.Value = new LocalisationParameters(localeMapping.Storage, configPreferUnicode.Value);
            }
            else
            {
                if (locale.OldValue == locale.NewValue)
                    // equal values mean invalid locale on startup, no real way to recover other than to set to default.
                    configLocale.SetDefault();
                else
                    // revert to the old locale if the new one is invalid.
                    configLocale.Value = locale.OldValue;
            }
        }

        /// <summary>
        /// Updates <see cref="currentParameters"/> to the <paramref name="locale"/> matching one of the <see cref="locales"/>.
        /// </summary>
        /// <param name="locale">Current <see cref="FrameworkSetting.Locale"/>, can be <see cref="string.Empty"/> to return <see cref="SystemDefaultLocaleMapping"/>.</param>
        /// <param name="localeMapping">If valid, <see cref="LocaleMapping"/> with <see cref="LocaleMapping.Name"/> equal to <paramref name="locale"/>, or <see cref="SystemDefaultLocaleMapping"/>.</param>
        /// <returns><c>true</c> if the requested <paramref name="locale"/> is valid was found in <see cref="locales"/>.</returns>
        private bool tryGetLocaleMappingForLocale(string locale, [NotNullWhen(true)] out LocaleMapping? localeMapping)
        {
            Debug.Assert(locales.Count > 0);

            if (string.IsNullOrEmpty(locale))
            {
                localeMapping = SystemDefaultLocaleMapping.AsNonNull(); // will not be null as there is at least one mapping.
                return true;
            }

            return locales.TryGetValue(locale, out localeMapping);
        }

        /// <summary>
        /// Gets the <see cref="LocaleMapping"/> from <see cref="locales"/> that most closely matches <see cref="CultureInfoHelper.SystemUICulture"/>,
        /// or <see cref="firstLocale"/> if none match.
        /// </summary>
        /// <returns>A <see cref="LocaleMapping"/> from <see cref="locales"/>, or <c>null</c> iff <see cref="locales"/> is empty.</returns>
        private LocaleMapping? getSystemDefaultLocaleMapping()
        {
            // also look for any parent (country-neutral) cultures that match. eg. `en-GB` will match `en` LocaleMapping.
            foreach (var c in CultureInfoHelper.SystemUICulture.EnumerateParentCultures())
            {
                if (locales.TryGetValue(c.Name, out var localeMapping))
                    return localeMapping;
            }

            return firstLocale;
        }

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
