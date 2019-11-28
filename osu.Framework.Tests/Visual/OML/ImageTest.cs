// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.OML;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.OML
{
    public class ImageTest : TestScene
    {
        private const string test_data = "<oml>" +
                                         "<sprite texture=\"https://a.ppy.sh/10291354\" width=\"250\" height=\"250\"/>" +
                                         "</oml>";

        private const string blur_test_data = "<oml>" +
                                              "    <bufferedContainer blurSigma=\"4,-4\">" +
                                              "        <sprite texture=\"https://a.ppy.sh/10291354\" " +
                                              "            width=\"250\" " +
                                              "            height=\"250\" " +
                                              "        />" +
                                              "    </bufferedContainer>" +
                                              "</oml>";

        [Test]
        public void TestImage()
        {
            AddStep("Render Image", performImageRender);
            AddStep("Blur Image", performBlurImage);
        }

        private void performImageRender()
        {
            var parser = new OmlParser(test_data);
            var display = new OmlDisplayContainer(parser);

            Child = display;
        }

        private void performBlurImage()
        {
            var parser = new OmlParser(blur_test_data);
            var display = new OmlDisplayContainer(parser);

            Child = display;
        }
    }
}
