using osu.Framework.Graphics.Containers;

namespace osu.Framework.OML
{
    public class OmlDisplayContainer : Container
    {
        public OmlDisplayContainer(IOmlParser parser)
        {
            AddRange(parser.ConstructContainers());
        }
    }
}