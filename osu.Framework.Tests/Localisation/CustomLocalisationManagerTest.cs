// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [HeadlessTest]
    public class CustomLocalisationManagerTest : LocalisationTestScene
    {
        private readonly BindableBool useCustomDateFormatBindable = new BindableBool();

        protected override LocalisationManager CreateLocalisationManager(FrameworkConfigManager config) => new CustomLocalisationManager(config, useCustomDateFormatBindable);

        [BackgroundDependencyLoader]
        private void load()
        {
            Manager.AddLocaleMappings(new LocaleMapping(new TestLocalisationStore(new CultureInfo("en-US", false), new Dictionary<string, string>())).Yield());
        }

        private static readonly DateTimeOffset date_time = new DateTimeOffset(2022, 11, 22, 7, 2, 7, new TimeSpan(1, 0, 0));

        [TestCase("D", "Tuesday, November 22, 2022", "2022 November 22")]
        [TestCase("y", "November 2022", "Nov 2022")]
        public void TestFormat(string format, string expected, string expectedCustom)
        {
            ILocalisedBindableString localised = null!;
            AddStep("get localised string", () => localised = Manager.GetLocalisedBindableString(date_time.ToLocalisableString(format)));

            AddStep("set useCustomDateFormat to false", () => useCustomDateFormatBindable.Value = false);
            AddAssert("localised text matches expected", () => localised.Value, () => Is.EqualTo(expected));

            AddStep("set useCustomDateFormat to true", () => useCustomDateFormatBindable.Value = true);
            AddAssert("localised text matches expected", () => localised.Value, () => Is.EqualTo(expectedCustom));
        }

        private class CustomLocalisationManager : LocalisationManager
        {
            private readonly BindableBool useCustomDateFormat = new BindableBool();

            public CustomLocalisationManager(FrameworkConfigManager config, BindableBool useCustomDateFormat)
                : base(config)
            {
                this.useCustomDateFormat.BindTo(useCustomDateFormat);
                this.useCustomDateFormat.BindValueChanged(_ => RefreshCulture());
            }

            protected override IFormatProvider CustomiseCultureAndFormatProvider(CultureInfo culture)
            {
                return new CustomFormatProvider(culture, useCustomDateFormat.Value);
            }

            protected override void Dispose(bool disposing)
            {
                useCustomDateFormat.UnbindAll();
                base.Dispose(disposing);
            }
        }

        private class CustomFormatProvider : ICustomFormatter, IFormatProvider
        {
            private readonly CultureInfo culture;
            private readonly bool useCustomDateFormat;

            public CustomFormatProvider(CultureInfo culture, bool useCustomDateFormat)
            {
                this.culture = culture;
                this.useCustomDateFormat = useCustomDateFormat;

                if (useCustomDateFormat)
                    culture.DateTimeFormat.LongDatePattern = "yyyy MMMM dd";
            }

            object? IFormatProvider.GetFormat(Type? formatType)
            {
                if (formatType == typeof(ICustomFormatter))
                    return this;

                return culture.GetFormat(formatType);
            }

            string ICustomFormatter.Format(string? format, object? arg, IFormatProvider? formatProvider)
            {
                if (useCustomDateFormat && arg is DateTimeOffset dateTime)
                {
                    switch (format)
                    {
                        case "y":
                            return dateTime.ToString("MMM yyyy", culture);
                    }
                }

                if (arg is IFormattable formattable)
                    return formattable.ToString(format, culture);

                return arg?.ToString() ?? string.Empty;
            }
        }
    }
}
