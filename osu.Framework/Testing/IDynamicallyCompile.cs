// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Testing
{
    /// <summary>
    /// A class which can be recompiled at runtime to allow for rapid testing.
    /// </summary>
    public interface IDynamicallyCompile
    {
        /// <summary>
        /// A list of types which may be edited and should be included during recompilation.
        /// </summary>
        IReadOnlyList<Type> RequiredTypes { get; }
    }
}
