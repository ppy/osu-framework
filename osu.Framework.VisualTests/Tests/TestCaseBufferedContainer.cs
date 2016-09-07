//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


namespace osu.Framework.VisualTests.Tests
{
    class TestCaseBufferedContainer : TestCase
    {
        internal override string Name => @"Buffered Containers";

        internal override string Description => @"Nested buffered containers.";

        internal override void Reset()
        {
            base.Reset();

            Container lastContainer;
            Add(lastContainer = new Container() { Size = new Vector2(300, 300) });

            for (int i = 0; i < 10; i++)
            {
                Container newContainer;
                if (i % 2 == 0)
                    lastContainer.Add(lastContainer = new BufferedContainer() { Depth = 1.0f, TagNumeric = i + 1 });
                else
                    lastContainer.Add(lastContainer = new Container() { Depth = 1.0f, TagNumeric = i + 1 });

                lastContainer.SizeMode = InheritMode.Inherit;
                lastContainer.PositionMode = PositionMode.Inherit;

                lastContainer.Position = new Vector2(0.1f, 0.1f);
                lastContainer.Size = new Vector2(0.8f, 0.8f);

                lastContainer.Add(new Box(new Color4((float)RNG.NextDouble(), (float)RNG.NextDouble(), (float)RNG.NextDouble(), 1.0f))
                {
                    SizeMode = InheritMode.Inherit
                });
            }
        }
    }
}
