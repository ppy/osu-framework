// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly IBindable<ILocalisationStore> storage = new Bindable<ILocalisationStore>();
            private readonly IBindable<bool> preferUnicode = new Bindable<bool>();

            private LocalisableString text;

            public LocalisedBindableString(LocalisableString text, IBindable<ILocalisationStore> storage, IBindable<bool> preferUnicode)
            {
                this.text = text;

                this.storage.BindTo(storage);
                this.preferUnicode.BindTo(preferUnicode);

                this.storage.BindValueChanged(_ => updateValue());
                this.preferUnicode.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                Value = text.Data switch
                {
                    string plain => plain,
                    RomanisableString romanisable => romanisable.GetPreferred(preferUnicode.Value),
                    TranslatableString translatable => translatable.Format(storage.Value),
                    _ => string.Empty,
                };
            }

            LocalisableString ILocalisedBindableString.Text
            {
                set
                {
                    if (text == value)
                        return;

                    text = value;

                    updateValue();
                }
            }
        }
    }
}
