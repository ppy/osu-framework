// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.SourceGeneration.Generators.UnbindAllBindables
{
    public readonly struct BindableDefinition
    {
        public readonly string Name;
        public readonly string FullyQualifiedContainingType;
        public readonly bool IsNullable;

        public BindableDefinition(string name, string fullyQualifiedContainingType, bool isNullable)
        {
            Name = name;
            FullyQualifiedContainingType = fullyQualifiedContainingType;
            IsNullable = isNullable;
        }
    }
}
