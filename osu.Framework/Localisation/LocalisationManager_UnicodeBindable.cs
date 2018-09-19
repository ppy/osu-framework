// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class UnicodeBindableString : Bindable<string>, IUnicodeBindableString
        {
            private readonly IBindable<bool> preferUnicode = new Bindable<bool>();

            private string unicodeText;
            private string nonUnicodeText;

            public UnicodeBindableString(string unicodeText, string nonUnicodeText, IBindable<bool> preferUnicode)
            {
                this.unicodeText = unicodeText;
                this.nonUnicodeText = nonUnicodeText;
                this.preferUnicode.BindTo(preferUnicode);
                this.preferUnicode.BindValueChanged(_ => updateValue(), true);
            }

            string IUnicodeBindableString.UnicodeText
            {
                set
                {
                    if (unicodeText == value)
                        return;
                    unicodeText = value;

                    updateValue();
                }
            }

            string IUnicodeBindableString.NonUnicodeText
            {
                set
                {
                    if (nonUnicodeText == value)
                        return;
                    nonUnicodeText = value;

                    updateValue();
                }
            }

            private void updateValue()
            {
                if (preferUnicode.Value)
                    Value = unicodeText ?? nonUnicodeText ?? string.Empty;
                else
                    Value = nonUnicodeText ?? unicodeText ?? string.Empty;
            }
        }
    }
}
