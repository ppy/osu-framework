// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A bindable string constructed from <see cref="string.Format(string, object[])"/>.
    /// </summary>
    public class FormatString : Bindable<string>
    {
        private readonly FormattableString formattable;

        public FormatString(FormattableString formattable)
        {
            this.formattable = formattable;
            Update();
        }

        public void Update()
        {
            Value = formattable.ToString();
        }
    }
}
