// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Allocation
{
    public struct CacheInfo
    {
        /// <summary>
        /// The name of the cached member.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The type containing the cached member.
        /// </summary>
        public readonly Type Parent;

        /// <summary>
        /// The type of the cached member.
        /// </summary>
        internal Type Type;

        public CacheInfo(string name = null, Type parent = null)
        {
            Type = null;
            Name = name;
            Parent = parent;
        }

        public override string ToString() => $"{nameof(Type)} = {Type?.ReadableName()}, {nameof(Name)} = {Name}, {nameof(Parent)} = {Parent?.ReadableName()}";
    }
}
