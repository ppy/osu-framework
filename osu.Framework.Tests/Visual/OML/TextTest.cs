using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class TextTest : TestScene
    {
        private const string test_data = "<oml>" +
                                         "<text height=\"250\" width=\"250\" fontSize=\"16\">welcome to osu!</text>" +
                                         "</oml>";

        [Test]
        public void TestText()
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
