// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Benchmarks
{
    public partial class BenchmarkTextFlowContainer : GameBenchmark
    {
        private TestGame game = null!;

        [Test]
        [Benchmark]
        public void SetText()
        {
            game.Schedule(() => game.TextFlow.Text =
                @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer in urna faucibus, consectetur lorem at, maximus lectus. Vivamus mattis faucibus ante et volutpat. Quisque ligula velit, tristique condimentum placerat sit amet, varius a nisi. Curabitur sit amet sodales ex. Cras in risus sed ipsum auctor laoreet. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Etiam sed ligula cursus neque gravida pretium non vel lacus. Aliquam tincidunt congue est nec malesuada.

Sed viverra neque sed orci ultrices varius. Suspendisse malesuada vitae ligula ornare laoreet. Nunc gravida, orci tempus placerat tempor, sem metus hendrerit nisl, in posuere erat turpis at felis. Pellentesque tempor velit odio, eu scelerisque odio sodales non. Cras sagittis, lorem ut tristique euismod, lacus lacus consequat lorem, vel placerat diam est luctus dolor. Duis ac erat tortor. Ut massa massa, iaculis porttitor sem vitae, sodales rhoncus risus. Aliquam erat volutpat. Ut sed est leo. Curabitur congue, dolor non suscipit molestie, neque nisi convallis erat, nec interdum nibh lacus nec dolor. Donec pellentesque commodo erat. Fusce sed ligula at elit mollis congue in sit amet quam. Maecenas venenatis cursus erat vitae ultrices. Etiam lobortis pellentesque accumsan. Curabitur blandit purus hendrerit arcu tempor venenatis.

Nunc ac commodo magna, et blandit lectus. Mauris sagittis urna vulputate massa blandit consectetur. Cras eget volutpat quam. Suspendisse a pharetra urna. Nunc sit amet ultricies ligula, eget blandit risus. Nunc mollis, nisl ut hendrerit consequat, eros ligula iaculis neque, sed viverra dolor felis vel purus. Vivamus eu sollicitudin nunc. Aliquam varius, nulla vitae efficitur placerat, tortor est eleifend justo, vitae venenatis urna diam nec velit. Integer in aliquet sapien, mollis tincidunt dui. Curabitur vestibulum pretium lorem. Morbi tincidunt semper aliquet. Curabitur semper est ac dapibus scelerisque. Morbi aliquam pulvinar bibendum. Curabitur blandit consequat sapien non faucibus. ");
            RunSingleFrame();
        }

        protected override Game CreateGame() => game = new TestGame();

        private partial class TestGame : Game
        {
            public TextFlowContainer TextFlow { get; private set; } = null!;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Add(TextFlow = new TextFlowContainer
                {
                    RelativeSizeAxes = Axes.Both
                });
            }
        }
    }
}
