// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A Bindable string which takes a unicode and non-unicode (usually romanised) version of the contained text
    /// and provides automatic switching behaviour should the user change their preference.
    /// </summary>
    public class UnicodeBindableString : Bindable<string>
    {
        public readonly string Unicode;
        public readonly string NonUnicode;

        public UnicodeBindableString(string unicode, string nonUnicode) : base(nonUnicode)
        {
            Unicode = unicode;
            NonUnicode = nonUnicode;

            if (Unicode == null)
                Unicode = NonUnicode;
            if (NonUnicode == null)
                NonUnicode = Unicode;
        }

        public bool PreferUnicode
        {
            get { return Value == Unicode; }
            set { Value = value ? Unicode : NonUnicode; }
        }
    }
}
