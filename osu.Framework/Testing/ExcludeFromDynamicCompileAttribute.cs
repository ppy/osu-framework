// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Indicates that a type should be excluded from dynamic compilation. Does not affect derived types.
    /// </summary>
    /// <remarks>
    /// This should be used as sparingly as possible for cases where compiling a type changes fundamental testing components (e.g. <see cref="TestBrowser"/>).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false)]
    public class ExcludeFromDynamicCompileAttribute : Attribute
    {
    }
}
