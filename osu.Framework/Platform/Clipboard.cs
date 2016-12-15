// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Platform
{
    public abstract class Clipboard
    {
        public abstract string GetText();

        public abstract void SetText(string selectedText);
    }
}
