// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A bindable string constructed from <see cref="string.Format(string, object[])"/>, and <see cref="Format"/> changable.
    /// </summary>
    public class VaraintFormattableString : FormattableString
    {
        private readonly Bindable<string> formatSource;
        protected override string Format => formatSource.Value;

        public VaraintFormattableString(Bindable<string> formatSource, params object[] objects)
            : base(null, objects)
        {
            this.formatSource = formatSource;
        }
    }
}
