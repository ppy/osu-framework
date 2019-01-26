// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;

namespace osu.Framework.Testing.Dependencies
{
    /// <summary>
    /// Used for internal <see cref="DependencyContainer"/> testing purposes.
    /// </summary>
    public class CachedNullableProvider
    {
        [Cached]
        private int? cachedObject = 5;

        public int? CachedObject => cachedObject;

        public void SetValue(int? value) => cachedObject = value;
    }
}
