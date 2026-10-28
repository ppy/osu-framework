// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Visualisation
{
    /// <summary>
    /// Indicates that instances of this type or any subtype should not be valid targets for the draw visualiser.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DrawVisualiserHiddenAttribute : Attribute;
}
