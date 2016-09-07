//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseFilters : TestCase
    {
        internal override string Name => @"Filters";

        internal override string Description => @"Filters applied to containers";

        internal override void Reset()
        {
            base.Reset();

            BloomFilter bloom = new BloomFilter();
            bloom.Magnification.Value = 0.01f;
            bloom.Alpha.Value = 0.14f;
            bloom.RedTint.Value = 0f;
            bloom.HiRange.Value = false;

            FlowContainer flow;
            Add(flow = new FlowContainer(FlowDirection.VerticalOnly)
            {
                Padding = new Vector2(0, 5)
            });

            Container c;
            flow.Add(c = new Container());
            c.Add(new SpriteText("Normal", 100));

            FilterContainer fc;
            flow.Add(fc = new FilterContainer());
            fc.Add(new SpriteText("Bloom", 100));
            fc.AddFilter(new PassthroughFilter());
            fc.AddFilter(new ScreenDrawFilter());
            fc.AddFilter(bloom);

            flow.Add(fc = new FilterContainer());
            fc.Add(new SpriteText("Pseudo-Blur", 50));
            for (int i = 0; i < 50; i++)
                fc.AddFilter(new PassthroughFilter());
        }
    }
}
