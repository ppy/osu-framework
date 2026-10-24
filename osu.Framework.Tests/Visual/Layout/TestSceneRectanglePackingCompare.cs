// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Framework.Utils.RectanglePacking;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public partial class TestSceneRectanglePackingCompare : FrameworkTestScene
    {
        private int minWidth = 1;
        private int maxWidth = 1000;
        private int minHeight = 1;
        private int maxHeight = 1000;
        private int binSize = 1000;
        private int runCount = 10;

        private List<IRectanglePacker> packers = null!;
        private Results results = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddSliderStep("Bin Size", 1, 2000, 1000, s => binSize = s);
            AddSliderStep("Min width", 1, 1000, 1, w => minWidth = w);
            AddSliderStep("Max width", 2, 1000, 250, h => maxWidth = h);
            AddSliderStep("Min height", 1, 1000, 1, w => minHeight = w);
            AddSliderStep("Max height", 2, 1000, 250, h => maxHeight = h);
            AddSliderStep("Run Count", 1, 100, 10, c => runCount = c);

            AddStep("Run", run);
        }

        private void run()
        {
            packers = new List<IRectanglePacker>
            {
                new ShelfRectanglePacker(new Vector2I(binSize)),
                new ShelfWithRemainderRectanglePacker(new Vector2I(binSize)),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.First, SplitStrategy.ShorterAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.First, SplitStrategy.ShorterLeftoverAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.SmallestArea, SplitStrategy.ShorterAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.SmallestArea, SplitStrategy.ShorterLeftoverAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.TopLeft, SplitStrategy.ShorterAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.TopLeft, SplitStrategy.ShorterLeftoverAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.BestShortSide, SplitStrategy.ShorterAxis),
                new GuillotineRectanglePacker(new Vector2I(binSize), FitStrategy.BestShortSide, SplitStrategy.ShorterLeftoverAxis),
                new MaximalRectanglePacker(new Vector2I(binSize), FitStrategy.First),
                new MaximalRectanglePacker(new Vector2I(binSize), FitStrategy.TopLeft),
                new MaximalRectanglePacker(new Vector2I(binSize), FitStrategy.BestLongSide),
                new MaximalRectanglePacker(new Vector2I(binSize), FitStrategy.BestShortSide),
                new MaximalRectanglePacker(new Vector2I(binSize), FitStrategy.SmallestArea),
            };

            results = new Results(packers);

            for (int i = 0; i < runCount; i++)
            {
                var currentRun = new Dictionary<IRectanglePacker, RunInfo>();

                foreach (var packer in packers)
                    currentRun.Add(packer, new RunInfo());

                addUntilAllFull(currentRun);
                results.LoadRun(currentRun);

                foreach (var packer in packers)
                    packer.Reset();
            }

            Child = new ResultsTable(results);
        }

        private void addUntilAllFull(Dictionary<IRectanglePacker, RunInfo> currentRun)
        {
            bool allFull = false;

            while (!allFull)
                allFull = addToAll(new Vector2I(RNG.Next(minWidth, maxWidth), RNG.Next(minHeight, maxHeight)), currentRun);
        }

        private bool addToAll(Vector2I toAdd, Dictionary<IRectanglePacker, RunInfo> currentRun)
        {
            bool added = false;

            foreach (var packer in packers)
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                Vector2I? pos = packer.TryAdd(toAdd.X, toAdd.Y);
                stopwatch.Stop();

                if (pos.HasValue)
                    currentRun[packer].OnRectangleAdded(stopwatch.Elapsed.Ticks);

                added |= pos.HasValue;
            }

            return !added;
        }

        private partial class ResultsTable : CompositeDrawable
        {
            private readonly Results results;

            public ResultsTable(Results results)
            {
                this.results = results;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                float maxCount = results.BestFit;
                double bestAverage = results.BestAverageTime;

                RelativeSizeAxes = Axes.Both;
                InternalChild = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 30),
                        new Dimension()
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Relative, 1f)
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Relative, size: 1)
                                },
                                ColumnDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Absolute, 400),
                                    new Dimension(GridSizeMode.Distributed, 0.5f),
                                    new Dimension(GridSizeMode.Distributed, 0.5f),
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Text = "Name",
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre
                                        },
                                        new SpriteText
                                        {
                                            Text = "Packed rectangles (avg)",
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre
                                        },
                                        new SpriteText
                                        {
                                            Text = "Time to add (avg)",
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                Child = new BasicScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer<DrawableResult>
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 5),
                                        Children = results.Dictionary.Values.OrderByDescending(r => r.AverageCount()).Select(r => new DrawableResult(r, maxCount, bestAverage)).ToArray()
                                    }
                                }
                            }
                        }
                    }
                };
            }

            private partial class DrawableResult : CompositeDrawable
            {
                private readonly RunsInfo info;
                private readonly float maxCount;
                private readonly double bestAverage;

                public DrawableResult(RunsInfo info, float maxCount, double bestAverage)
                {
                    this.info = info;
                    this.maxCount = maxCount;
                    this.bestAverage = bestAverage;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    RelativeSizeAxes = Axes.X;
                    Height = 30;
                    InternalChild = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        RowDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Relative, size: 1)
                        },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 400),
                            new Dimension(GridSizeMode.Distributed, 0.5f),
                            new Dimension(GridSizeMode.Distributed, 0.5f),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new SpriteText
                                {
                                    Text = info.Name,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    RelativeSizeAxes = Axes.X,
                                    Truncate = true
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 5 },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Width = info.AverageCount() / maxCount,
                                            Colour = Color4.Gray
                                        },
                                        new SpriteText
                                        {
                                            Text = info.AverageCount().ToString(CultureInfo.InvariantCulture),
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Shadow = true
                                        }
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 5 },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Width = (float)(1.0 - bestAverage / info.AverageTime()),
                                            Colour = Color4.Gray
                                        },
                                        new SpriteText
                                        {
                                            Text = info.AverageTime().ToString(CultureInfo.InvariantCulture),
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Shadow = true
                                        }
                                    }
                                }
                            }
                        }
                    };
                }
            }
        }

        private class Results
        {
            public readonly Dictionary<IRectanglePacker, RunsInfo> Dictionary = new Dictionary<IRectanglePacker, RunsInfo>();

            public Results(IEnumerable<IRectanglePacker> packers)
            {
                foreach (var packer in packers)
                {
                    Dictionary.Add(packer, new RunsInfo(packer.ToString() ?? string.Empty));
                }
            }

            public void LoadRun(Dictionary<IRectanglePacker, RunInfo> runResults)
            {
                foreach (var r in runResults)
                    Dictionary[r.Key].AddRunInfo(r.Value);
            }

            public float BestFit
            {
                get
                {
                    float max = 0;

                    foreach (var info in Dictionary.Values)
                        max = Math.Max(max, info.AverageCount());

                    return max;
                }
            }

            public double BestAverageTime
            {
                get
                {
                    double min = double.MaxValue;

                    foreach (var info in Dictionary.Values)
                        min = Math.Min(min, info.AverageTime());

                    return min;
                }
            }
        }

        private class RunsInfo
        {
            public string Name { get; }

            private List<RunInfo> runInfos { get; } = new List<RunInfo>();

            public RunsInfo(string name)
            {
                Name = name;
            }

            public void AddRunInfo(RunInfo info)
            {
                runInfos.Add(info);
            }

            public float AverageCount()
            {
                int sum = 0;

                foreach (var info in runInfos)
                    sum += info.RectangleCount;

                return sum / (float)runInfos.Count;
            }

            public double AverageTime()
            {
                double sum = 0;

                foreach (var info in runInfos)
                    sum += info.AverageTime();

                return sum / runInfos.Count;
            }
        }

        private class RunInfo
        {
            public int RectangleCount { get; private set; }

            private readonly List<long> timings = new List<long>();

            public void OnRectangleAdded(long time)
            {
                RectangleCount++;
                timings.Add(time);
            }

            public double AverageTime() => timings.Average();
        }
    }
}
