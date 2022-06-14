// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;

namespace osu.Framework.Testing.Dependencies
{
    /// <summary>
    /// Used for internal <see cref="DependencyContainer"/> testing purposes.
    /// </summary>
    public class CachedNullableProvider
    {
        [Cached]
        public int? CachedObject { get; private set; } = 5;

        public void SetValue(int? value) => CachedObject = value;
    }
}
