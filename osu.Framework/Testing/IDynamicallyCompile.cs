// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Testing
{
    /// <summary>
    /// A class which can be recompiled at runtime to allow for rapid testing.
    /// </summary>
    internal interface IDynamicallyCompile
    {
        /// <summary>
        /// A reference to the original instance which dynamic compilation was based on.
        /// Will reference self if already the original.
        /// </summary>
        object DynamicCompilationOriginal { get; }
    }
}
