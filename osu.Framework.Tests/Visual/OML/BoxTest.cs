using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class BoxTest : TestScene
    {
        private const string test_data = "<oml>" +
                                         "<Box width=\"250\" height=\"250\" color=\"#FF0000\"   pos=\"0,0\"    />" +
                                         "<Box width=\"250\" height=\"250\" color=\"green\"     pos=\"100,100\"/>" +
                                         "<Box width=\"250\" height=\"250\" color=\"#AF0000FF\" pos=\"200,200\"/>" +
                                         "</oml>";

        [Test]
        public void TestBox()
        {
            AddStep("Parse OML", performParse);
        }

        private void performParse()
        {
            var parser = new OmlParser(test_data);
            var display = new OmlDisplayContainer(parser);

            Child = display;
        }
    }
}
