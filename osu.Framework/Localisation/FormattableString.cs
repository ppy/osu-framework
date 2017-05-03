// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A bindable string constructed from <see cref="string.Format(string, object[])"/>.
    /// </summary>
    public class FormattableString : Bindable<string>
    {
        private readonly string format;
        private readonly object[] objects;
        protected virtual string Format => format;
        public void Update() => Value = string.Format(Format, objects);

        public FormattableString(string format, params object[] objects)
        {
            this.format = format;
            this.objects = objects;
            Update();
        }
    }
}
