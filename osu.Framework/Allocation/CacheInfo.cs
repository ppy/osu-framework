// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Allocation
{
    public readonly struct CacheInfo : IEquatable<CacheInfo>
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
        internal readonly Type Type;

        public CacheInfo(string name = null, Type parent = null)
            : this(null, name, parent)
        {
        }

        private CacheInfo(Type type, string name, Type parent)
        {
            Type = type;
            Name = name;
            Parent = parent;
        }

        internal CacheInfo WithType(Type type) => new CacheInfo(type, Name, Parent);

        public override string ToString() => $"{nameof(Type)} = {Type?.ReadableName()}, {nameof(Name)} = {Name}, {nameof(Parent)} = {Parent?.ReadableName()}";

        public override bool Equals(object obj) => obj is CacheInfo cacheInfo && Equals(cacheInfo);

        public bool Equals(CacheInfo other) => Name == other.Name && Parent == other.Parent && Type == other.Type;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Parent != null ? Parent.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
