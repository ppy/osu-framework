// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneLatency : TestScene
    {
        private GameHost host = null!;

        private Bindable<FrameSync> frameSync = null!;
        private Bindable<ExecutionMode> executionMode = null!;

        private SpriteText maximumDrawHz = null!;
        private SpriteText maximumUpdateHz = null!;
        private SpriteText frameSyncText = null!;
        private SpriteText executionModeText = null!;

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config, GameHost host)
        {
            this.host = host;

            Children =
            [
                new FlashingBox(),
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new osuTK.Vector2(0, 5),
                    Children =
                    [
                        new SpriteText { Text = "Click anywhere to flash the screen white.", },
                        maximumDrawHz = new SpriteText(),
                        maximumUpdateHz = new SpriteText(),
                        frameSyncText = new SpriteText(),
                        executionModeText = new SpriteText(),
                    ]
                },
            ];

            frameSync = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            executionMode = config.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);

            AddStep("Frame sync: VSync", () => frameSync.Value = FrameSync.VSync);
            AddStep("Frame sync: VRR", () => frameSync.Value = FrameSync.VSyncVRR);
            AddStep("Frame sync: Limit2x", () => frameSync.Value = FrameSync.Limit2x);
            AddStep("Frame sync: Limit4x", () => frameSync.Value = FrameSync.Limit4x);
            AddStep("Frame sync: Limit8x", () => frameSync.Value = FrameSync.Limit8x);
            AddStep("Frame sync: Unlimited", () => frameSync.Value = FrameSync.Unlimited);
            AddStep("Execution mode: MultiThreaded", () => executionMode.Value = ExecutionMode.MultiThreaded);
            AddStep("Execution mode: SingleThread", () => executionMode.Value = ExecutionMode.SingleThread);

            AddSliderStep("Extra GPU load", 0, 1000, 0, adjustFillBoxCount);
        }

        private void adjustFillBoxCount(int count)
        {
            while (Content.Count > count + 2)
                Content.Remove(Content.Children.Last(), true);

            while (Content.Count < count + 2)
            {
                Content.Add(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(0, 0, 0, 0),
                });
            }
        }

        protected override void Update()
        {
            maximumDrawHz.Text = $"Maximum Draw Hz: {host.MaximumDrawHz:N2}";
            maximumUpdateHz.Text = $"Maximum Update Hz: {host.MaximumUpdateHz:N2}";
            frameSyncText.Text = $"Frame sync: {frameSync.Value}";
            executionModeText.Text = $"Execution mode: {executionMode.Value}";
        }

        private partial class FlashingBox : Box
        {
            public FlashingBox()
            {
                RelativeSizeAxes = Axes.Both;
                Colour = Color4.Black;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                Colour = Color4.White;
                return base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                Colour = Color4.Black;
                base.OnMouseUp(e);
            }
        }
    }
}
