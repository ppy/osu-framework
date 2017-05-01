// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Containers
{
    public interface IFilterable
    {
        /// <summary>
        /// An array of relevant terms which match the current object in a filtered scenario.
        /// </summary>
        string[] FilterTerms { get; }

        /// <summary>
        /// Whether the current object is matching (ie. visible) given the current filter criteria of a parent.
        /// </summary>
        bool MatchingCurrentFilter { set; }
    }
}
