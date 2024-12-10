// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Transforms
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TransformGeneratorAttribute : Attribute
    {
        public readonly string? Name;
        public string? Grouping { get; set; }

        public TransformGeneratorAttribute()
        {
        }

        public TransformGeneratorAttribute(string name)
        {
            Name = name;
        }
    }
}
