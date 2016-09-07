//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System.IO;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseReplay : TestCase
    {
        enum Mode
        {
            Mirror,
            AllReplays,
            SkippingTest
        }

        private Score score;
        internal override string Name => @"Replay";

        internal override string Description => @"Load and view lots of replays";

        internal override int DisplayOrder => -1;

        Mode mode = Mode.Mirror;

        internal override void Reset()
        {
            base.Reset();

            AddButton(@"Mirror Local Input", delegate
            {
                mode = Mode.Mirror;
                Reset();
            });

            AddButton(@"Load All Replays", delegate
            {
                mode = Mode.AllReplays;
                Reset();
            });

            AddButton(@"Test Frame Skipping", delegate
            {
                mode = Mode.SkippingTest;
                Reset();
            });

            ReplayInputManager inputManager;

            switch (mode)
            {
                case Mode.Mirror:
                    {
                        score = new Score();

                        Cursor cursor = new Cursor();
                        cursor.SetCursorSprites(
                            new Sprite(TextureManager.Load(@"cursor")) { Origin = Anchor.Centre },
                            new Sprite(TextureManager.Load(@"cursormiddle")) { Origin = Anchor.Centre });
                        cursor.Scale = 1.4f;
                        cursor.Colour = Color4.Orange;

                        inputManager = new ReplayInputManager(score);

                        inputManager.Add(cursor);

                        TestCaseTextBox tbtc = new TestCaseTextBox();
                        tbtc.Reset();
                        inputManager.Add(tbtc);

                        Add(inputManager);
                        break;
                    }
                case Mode.AllReplays:
                    {
                        TestCaseClocks clocks = new TestCaseClocks();
                        clocks.Reset();
                        Add(clocks);

                        string replayFolder = Path.Combine(Bootstrapper.UserPath, @"Data", @"r");

                        foreach (string replay in Directory.GetFiles(replayFolder, @"*.osr"))
                        {
                            try
                            {
                                score = ScoreManager.ReadReplayFromFile(replay);

                                Cursor cursor = new Cursor();
                                cursor.SetCursorSprites(
                                    new Sprite(TextureManager.Load(@"cursor")) { Origin = Anchor.Centre },
                                    new Sprite(TextureManager.Load(@"cursormiddle")) { Origin = Anchor.Centre });
                                cursor.Scale = 1.4f;
                                cursor.Colour = Color4.Orange;

                                inputManager = new ReplayInputManager(score);

                                inputManager.Add(cursor);

                                clocks.Container.Add(inputManager);
                            }
                            catch { }
                        }
                    }
                    break;
                case Mode.SkippingTest:
                    {
                        TestCaseClocks clocks = new TestCaseClocks();
                        clocks.Reset();
                        Add(clocks);

                        score = new Score();

                        Cursor cursor = new Cursor();
                        cursor.SetCursorSprites(
                            new Sprite(TextureManager.Load(@"cursor")) { Origin = Anchor.Centre },
                            new Sprite(TextureManager.Load(@"cursormiddle")) { Origin = Anchor.Centre });
                        cursor.Scale = 1.4f;
                        cursor.Colour = Color4.Orange;

                        inputManager = new ReplayInputManager(score);

                        inputManager.SizeMode = InheritMode.Fixed;
                        inputManager.Size = new Vector2(512, 384);
                        inputManager.Anchor = Anchor.Centre;
                        inputManager.Origin = Anchor.Centre;

                        score.Replay.Add(new bReplayFrame(0, 0, 0, pButtonState.None));
                        score.Replay.Add(new bReplayFrame(500, 200, 200, pButtonState.Left1));
                        score.Replay.Add(new bReplayFrame(510, 200, 200, pButtonState.None));

                        score.Replay.Add(new bReplayFrame(1000, 300, 200, pButtonState.Left1));
                        score.Replay.Add(new bReplayFrame(1010, 300, 200, pButtonState.None));

                        score.Replay.Add(new bReplayFrame(1500, 200, 300, pButtonState.Left1));
                        score.Replay.Add(new bReplayFrame(1510, 200, 300, pButtonState.None));

                        score.Replay.Add(new bReplayFrame(2000, 300, 300, pButtonState.Left1));
                        score.Replay.Add(new bReplayFrame(2010, 300, 300, pButtonState.None));

                        inputManager.Add(new ClickBox() { Position = new Vector2(200, 200) });
                        inputManager.Add(new ClickBox() { Position = new Vector2(300, 200) });
                        inputManager.Add(new ClickBox() { Position = new Vector2(200, 300) });
                        inputManager.Add(new ClickBox() { Position = new Vector2(300, 300) });

                        inputManager.Add(cursor);

                        clocks.Container.Add(inputManager);
                    }
                    break;
            }
        }

        const double delay = 10;

        protected override bool OnMouseMove(InputState state)
        {
            if (mode != Mode.Mirror) return false;

            Vector2 local = GetLocalPosition(state.Mouse.Position);

            score.Replay.Add(new bReplayFrame(Clock.CurrentTime + delay, local.X, local.Y, state.Mouse.LeftButton ? pButtonState.Left1 : pButtonState.None));
            return base.OnMouseMove(state);
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            if (mode != Mode.Mirror) return false;

            Vector2 local = GetLocalPosition(state.Mouse.Position);

            score.Replay.Add(new bReplayFrame(Clock.CurrentTime + delay, local.X, local.Y, state.Mouse.LeftButton ? pButtonState.Left1 : pButtonState.None));
            return base.OnMouseDown(state, args);
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            if (mode != Mode.Mirror) return false;

            Vector2 local = GetLocalPosition(state.Mouse.Position);

            score.Replay.Add(new bReplayFrame(Clock.CurrentTime + delay, local.X, local.Y, state.Mouse.LeftButton ? pButtonState.Left1 : pButtonState.None));
            return base.OnMouseUp(state, args);
        }
    }

    internal class ClickBox : Box
    {
        public ClickBox()
        {
            Size = new Vector2(20, 20);
            Colour = Color4.Red;
            Origin = Anchor.Centre;
        }

        protected override bool OnClick(InputState state)
        {
            ScaleTo(1.5f, 1000, EasingTypes.OutElastic);
            FadeColour(Color4.GreenYellow, 1000);
            return base.OnClick(state);
        }
    }
}
