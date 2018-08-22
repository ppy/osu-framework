// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
