// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    [Ignore("This test will never complete if working correctly. Test manually if required.")]
    public class ConvexPolygonClipperFuzzingTest
    {
        private const int parallelism = 4;

        private static readonly float[] possible_values =
        {
            float.NegativeInfinity,
            float.MinValue,
            float.MinValue / 2,
            -10.0f,
            -2.0f,
            -1.0f,
            -0.5f,
            -0.1f,
            0.0f,
            float.Epsilon,
            0.1f,
            0.5f,
            1.0f,
            2.0f,
            10.0f,
            float.MaxValue / 2,
            float.MaxValue,
            float.PositiveInfinity,
            float.NaN,
        };

        private static readonly int[] possible_sizes =
        {
            3,
            4,
            5,
            8,
            16
        };

        [Test]
        public void RunTest()
        {
            Task[] tasks = new Task[parallelism];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        int count1 = getRand(possible_sizes);
                        int count2 = getRand(possible_sizes);

                        HashSet<Vector2> vertices1 = new HashSet<Vector2>();
                        HashSet<Vector2> vertices2 = new HashSet<Vector2>();

                        while (vertices1.Count < count1)
                            vertices1.Add(new Vector2(getRand(possible_values), getRand(possible_values)));

                        while (vertices2.Count < count2)
                            vertices2.Add(new Vector2(getRand(possible_values), getRand(possible_values)));

                        SimpleConvexPolygon poly1 = new SimpleConvexPolygon(vertices1.ToArray());
                        SimpleConvexPolygon poly2 = new SimpleConvexPolygon(vertices2.ToArray());

                        try
                        {
                            clip(poly1, poly2);
                        }
                        catch (Exception ex)
                        {
                            Assert.Fail($"Failed.\nPoly1: {poly1}\nPoly2: {poly2}\n\nException: {ex}");
                            return;
                        }

                        try
                        {
                            clip(poly2, poly1);
                        }
                        catch (Exception ex)
                        {
                            Assert.Fail($"Failed.\nPoly1: {poly2}\nPoly2: {poly1}\n\nException: {ex}");
                            return;
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }

            Task.WaitAny(tasks);
        }

        private static Vector2[] clip(SimpleConvexPolygon poly1, SimpleConvexPolygon poly2)
        {
            var clipper = new ConvexPolygonClipper<SimpleConvexPolygon, SimpleConvexPolygon>(ref poly1, ref poly2);

            Span<Vector2> buffer = stackalloc Vector2[clipper.GetClipBufferSize()];

            return clipper.Clip(buffer).ToArray();
        }

        private static T getRand<T>(T[] arr) => arr[RNG.Next(0, arr.Length)];
    }
}
