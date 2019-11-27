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
