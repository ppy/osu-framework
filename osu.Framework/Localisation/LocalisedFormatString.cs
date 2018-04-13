// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A bindable string constructed from <see cref="string.Format(string, object[])"/>, and <see cref="Format"/> changable.
    /// </summary>
    public class LocalisedFormatString : FormattableString
    {
        private readonly Bindable<string> formatSource;
        private readonly object[] objects;

        public LocalisedFormatString(Bindable<string> formatSource, params object[] objects)
        {
            this.formatSource = formatSource;
            this.objects = objects;
        }

        public override string Format => formatSource.Value;

        public override int ArgumentCount => objects.Length;

        public override object GetArgument(int index) => objects[index];

        public override object[] GetArguments() => objects;

        public override string ToString(IFormatProvider formatProvider) => string.Format(formatProvider, formatSource.Value, objects);
    }
}
