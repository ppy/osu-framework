// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using Microsoft.CodeAnalysis;

namespace osu.Framework.Testing
{
    internal readonly struct AssemblyReference
    {
        public readonly Assembly Assembly;
        public readonly bool IgnoreAccessChecks;

        public AssemblyReference(Assembly assembly, bool ignoreAccessChecks)
        {
            Assembly = assembly;
            IgnoreAccessChecks = ignoreAccessChecks;
        }

        public MetadataReference GetReference() => MetadataReference.CreateFromFile(Assembly.Location);
    }
}
