// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#pragma warning disable 8632 // TODO: can be #nullable enable when Bindables are updated to also be.

using System.Globalization;
using osu.Framework.Bindables;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly IBindable<LocalisationParameters> parameters = new Bindable<LocalisationParameters>();

            private LocalisableString text;

            public LocalisedBindableString(LocalisableString text, IBindable<LocalisationParameters> parameters)
            {
                this.text = text;

                this.parameters.BindTo(parameters);
                this.parameters.BindValueChanged(_ => updateValue());

                updateValue();
            }

            private void updateValue()
            {
                switch (text.Data)
                {
                    case string plain:
                        Value = plain;
                        break;

                    case ILocalisableStringData data:
                        Value = applyCase(data.GetLocalised(parameters.Value));
                        break;

                    default:
                        Value = string.Empty;
                        break;
                }
            }

            private string applyCase(string data)
            {
                var cultureText = parameters.Value.Store?.EffectiveCulture?.TextInfo ?? CultureInfo.CurrentCulture.TextInfo;
                switch (text.Casing)
                {
                    case Casing.Uppercase:
                        return cultureText.ToUpper(data);

                    case Casing.TitleCase:
                        return cultureText.ToTitleCase(data);

                    case Casing.Lowercase:
                        return cultureText.ToLower(data);

                    case Casing.Default:
                    default:
                        return data;
                }
            }

            LocalisableString ILocalisedBindableString.Text
            {
                set
                {
                    if (text.Equals(value))
                        return;

                    text = value;

                    updateValue();
                }
            }
        }
    }
}
