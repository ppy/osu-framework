// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public partial class LocalisationEngine
    {
        private class UnicodeBindable : Bindable<string>
        {
            public readonly IBindable<bool> PreferUnicode = new Bindable<bool>();

            private readonly string unicode;
            private readonly string nonUnicode;

            public UnicodeBindable(string unicode, string nonUnicode)
                : base(nonUnicode)
            {
                this.unicode = unicode ?? nonUnicode;
                this.nonUnicode = nonUnicode ?? unicode;

                PreferUnicode.BindValueChanged(updateValue, true);
            }

            private void updateValue(bool preferUnicode) => Value = preferUnicode ? unicode : nonUnicode;
        }
    }
}
