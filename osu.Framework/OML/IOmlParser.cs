// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Xml.Linq;
using osu.Framework.OML.Objects;

namespace osu.Framework.OML
{
    public interface IOmlParser
    {
        IEnumerable<OmlObject> ConstructContainers();

        T ParseAttribute<T>(XAttribute attribute, T def = default)  where T : struct;
    }
}
