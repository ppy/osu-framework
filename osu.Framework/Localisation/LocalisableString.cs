// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public class LocalisableString
    {
        public Bindable<string> Text { get; set; }
        public Bindable<string> NonUnicode { get; set; }
        public Bindable<LocalisationType> Type { get; set; }
        public Bindable<object[]> Args { get; set; }

        public LocalisableString(string text, LocalisationType type, string nonUnicode = null, params object[] args)
        {
            Text = new Bindable<string>(text);
            NonUnicode = new Bindable<string>(nonUnicode);
            Type = new Bindable<LocalisationType>(type);
            Args = new Bindable<object[]>(args);
        }

        public static implicit operator LocalisableString(string unlocalised) => new LocalisableString(unlocalised, LocalisationType.None);
    }
}
