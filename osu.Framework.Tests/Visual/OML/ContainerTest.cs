// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class ContainerTest : TestScene
    {
        private const string test_data_1 = ""
                                           + "<oml>"
                                           + "    <container width=\"250\" height=\"250\">"
                                           + "        <box width=\"250\" height=\"250\" colour=\"orange\" />"
                                           + "        <sprite texture=\"https://a.ppy.sh/10291354\" width=\"200\" height=\"200\" margin=\"25\" />"
                                           + "    </container>"
                                           + "</oml>";

        [Test]
        public void TestNestedContainers()
        {
            AddStep("Parse OML", performParse);
        }

        private void performParse()
        {
            var parser = new OmlParser(test_data_1);
            var display = new OmlDisplayContainer(parser);

            Child = display;
        }
    }
}
