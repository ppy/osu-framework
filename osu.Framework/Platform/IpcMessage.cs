﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
namespace osu.Framework.Platform
{
    public class IpcMessage
    {
        public string Type { get; set; }
        public object Value { get; set; }
    }
}