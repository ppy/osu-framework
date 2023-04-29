// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.SourceGeneration.Generators.UnbindAllBindables
{
    public readonly struct BindableDefinition
    {
        public readonly string Name;
        public readonly string FullyQualifiedContainingType;

        public BindableDefinition(string name, string fullyQualifiedContainingType)
        {
            Name = name;
            FullyQualifiedContainingType = fullyQualifiedContainingType;
        }
    }
}
