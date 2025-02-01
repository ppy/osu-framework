// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.SourceGeneration.Generators.Transforms
{
    public readonly struct TransformMemberData
    {
        public readonly string MethodName;
        public readonly string? Grouping;
        public readonly string PropertyOrFieldName;
        public readonly string GlobalPrefixedTypeName;

        public TransformMemberData(string methodName, string? grouping, string propertyOrFieldName, string globalPrefixedTypeName)
        {
            MethodName = methodName;
            Grouping = grouping;
            PropertyOrFieldName = propertyOrFieldName;
            GlobalPrefixedTypeName = globalPrefixedTypeName;
        }
    }
}
