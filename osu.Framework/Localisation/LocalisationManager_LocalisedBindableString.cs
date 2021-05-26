// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#pragma warning disable 8632 // TODO: can be #nullable enable when Bindables are updated to also be.

using osu.Framework.Bindables;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly IBindable<ILocalisationStore?> storage = new Bindable<ILocalisationStore?>();
            private readonly IBindable<bool> preferUnicode = new Bindable<bool>();

            private LocalisableString text;

            public LocalisedBindableString(LocalisableString text, Bindable<ILocalisationStore?> storage, IBindable<bool> preferUnicode)
            {
                this.text = text;

                this.storage.BindTo(storage);
                this.preferUnicode.BindTo(preferUnicode);

                this.storage.BindValueChanged(_ => updateValue());
                this.preferUnicode.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                switch (text.Data)
                {
                    case string plain:
                        Value = plain;
                        break;

                    case RomanisableString romanisable:
                        Value = romanisable.GetPreferred(preferUnicode.Value);
                        break;

                    case TranslatableString translatable:
                        Value = translatable.Format(storage.Value);
                        break;

                    default:
                        Value = string.Empty;
                        break;
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
