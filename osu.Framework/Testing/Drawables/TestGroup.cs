// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Testing.Drawables
{
    public class TestGroup
    {
        public string Name { get; set; }
        public Type[] TestTypes { get; set; }
    }
}
