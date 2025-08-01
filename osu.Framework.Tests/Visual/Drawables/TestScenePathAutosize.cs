// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Lines;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestScenePathAutosize : TestScene
    {
        private Path path = null!;

        private readonly Vector2[] vertices =
        {
            new Vector2(-137.85623f, -30.258106f),
            new Vector2(-138.19534f, -30.35677f),
            new Vector2(-140.63135f, -30.975367f),
            new Vector2(-143.04555f, -31.448368f),
            new Vector2(-145.441f, -31.801311f),
            new Vector2(-147.81664f, -32.032097f),
            new Vector2(-150.16556f, -32.107544f),
            new Vector2(-152.49553f, -32.078487f),
            new Vector2(-154.79526f, -31.892113f),
            new Vector2(-157.06891f, -31.580147f),
            new Vector2(-159.3151f, -31.143425f),
            new Vector2(-161.52437f, -30.55334f),
            new Vector2(-163.70761f, -29.863062f),
            new Vector2(-165.84949f, -29.026585f),
            new Vector2(-167.95605f, -28.078064f),
            new Vector2(-170.0256f, -27.021856f),
            new Vector2(-172.0459f, -25.839844f),
            new Vector2(-174.03168f, -24.579554f),
            new Vector2(-175.96307f, -23.211433f),
            new Vector2(-177.84843f, -21.767414f),
            new Vector2(-179.68588f, -20.25596f),
            new Vector2(-181.4602f, -18.676243f),
            new Vector2(-183.19058f, -17.060707f),
            new Vector2(-184.85233f, -15.407352f),
            new Vector2(-186.45645f, -13.74036f),
            new Vector2(-188.00099f, -12.072891f),
            new Vector2(-189.46762f, -10.429077f),
            new Vector2(-190.88043f, -8.81538f),
            new Vector2(-192.20972f, -7.2699194f),
            new Vector2(-193.46945f, -5.802718f),
            new Vector2(-194.65762f, -4.4322186f),
            new Vector2(-195.75299f, -3.216076f),
            new Vector2(-196.7846f, -2.1223922f),
            new Vector2(-197.71788f, -1.24344f),
            new Vector2(-198.56982f, -0.5677992f),
            new Vector2(-199.33853f, -0.119781494f),
            new Vector2(-422.4743f, 40.28658f),
        };

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            path = createPath(vertices);
            Add(path);
        });

        [Test]
        public void TestPathAutosize()
        {
            AddAssert("Correct auto-sizing", () => path.Size == new Vector2(304.61807f, 92.39412f));
        }

        private static Path createPath(Vector2[] points) => new Path
        {
            Vertices = points,
        };
    }
}
