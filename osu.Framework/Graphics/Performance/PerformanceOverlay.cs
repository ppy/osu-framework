// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Platform.SDL2;
using osu.Framework.Platform.SDL3;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Performance
{
    internal partial class PerformanceOverlay : FillFlowContainer, IStateful<FrameStatisticsMode>
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private FrameworkConfigManager config { get; set; } = null!;

        private FrameStatisticsMode state;

        private TextFlowContainer? infoText;

        private Bindable<FrameSync> configFrameSync = null!;
        private Bindable<ExecutionMode> configExecutionMode = null!;
        private Bindable<WindowMode> configWindowMode = null!;

        public event Action<FrameStatisticsMode>? StateChanged;

        private bool initialised;

        private readonly List<FrameStatisticsDisplay> frameDisplays = new List<FrameStatisticsDisplay>();

        public FrameStatisticsMode State
        {
            get => state;
            set
            {
                if (state == value) return;

                state = value;

                if (IsLoaded)
                    updateState();
            }
        }

        public PerformanceOverlay()
        {
            Direction = FillDirection.Vertical;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            configFrameSync = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            configFrameSync.BindValueChanged(_ => updateInfoText());

            configExecutionMode = config.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);
            configExecutionMode.BindValueChanged(_ => updateInfoText());

            configWindowMode = config.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            configWindowMode.BindValueChanged(_ => updateInfoText());

            updateState();
            updateInfoText();
        }

        // for some reason PerformanceOverlay has 0 width despite using AutoSizeAxes, and it doesn't look simple to fix.
        // let's just work around it and consider frame statistics display dimensions for receiving input events.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            foreach (var display in frameDisplays)
            {
                if (display.ReceivePositionalInputAt(screenSpacePos))
                    return true;
            }

            return false;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            switch (e.Key)
            {
                case Key.ControlLeft:
                    applyToDisplays(static d => d.Expanded = true);
                    break;

                case Key.ShiftLeft:
                    applyToDisplays(static d => d.Running = false);
                    break;
            }

            return base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            switch (e.Key)
            {
                case Key.ControlLeft:
                    applyToDisplays(static d => d.Expanded = false);
                    break;

                case Key.ShiftLeft:
                    applyToDisplays(static d => d.Running = true);
                    break;
            }

            base.OnKeyUp(e);
        }

        protected override bool OnTouchDown(TouchDownEvent e)
        {
            switch (e.Touch.Source)
            {
                case TouchSource.Touch1:
                    applyToDisplays(static d => d.Expanded = true);
                    break;

                case TouchSource.Touch2:
                    applyToDisplays(static d => d.Running = false);
                    break;
            }

            return base.OnTouchDown(e);
        }

        protected override void OnTouchUp(TouchUpEvent e)
        {
            switch (e.Touch.Source)
            {
                case TouchSource.Touch1:
                    applyToDisplays(static d => d.Expanded = false);
                    break;

                case TouchSource.Touch2:
                    applyToDisplays(static d => d.Running = true);
                    break;
            }

            base.OnTouchUp(e);
        }

        private void updateState()
        {
            switch (state)
            {
                case FrameStatisticsMode.None:
                    this.FadeOut(100);
                    break;

                case FrameStatisticsMode.Minimal:
                case FrameStatisticsMode.Full:
                    if (!initialised)
                    {
                        initialised = true;

                        var uploadPool = createUploadPool();

                        Add(infoText = new TextFlowContainer(cp => cp.Font = FrameworkFont.Condensed)
                        {
                            Alpha = 0.75f,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            TextAnchor = Anchor.TopRight,
                            AutoSizeAxes = Axes.Both,
                        });

                        updateInfoText();

                        foreach (GameThread t in host.Threads)
                        {
                            var display = new FrameStatisticsDisplay(t, uploadPool)
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                State = state
                            };

                            Add(display);
                            frameDisplays.Add(display);
                        }
                    }

                    this.FadeIn(100);
                    break;
            }

            foreach (var display in frameDisplays)
                display.State = state;

            StateChanged?.Invoke(State);
        }

        private void applyToDisplays(Predicate<FrameStatisticsDisplay> predicate)
        {
            foreach (var display in frameDisplays)
                predicate.Invoke(display);
        }

        private void updateInfoText()
        {
            if (infoText == null)
                return;

            infoText.Clear();

            addHeader("Renderer:");
            addValue(host.RendererInfo);

            infoText.NewLine();

            addHeader("Limiter:");
            addValue(configFrameSync.ToString());
            addHeader("Execution:");
            addValue(configExecutionMode.ToString());
            addHeader("Mode:");
            addValue(configWindowMode.ToString());

            switch (host.Window)
            {
                case SDL3Window:
                    addValue(" (SDL3)");
                    break;

                case SDL2Window:
                    addValue(" (SDL2)");
                    break;
            }

            void addHeader(string text) => infoText.AddText($"{text} ", cp =>
            {
                cp.Padding = new MarginPadding { Left = 5 };
                cp.Colour = Color4.Gray;
            });

            void addValue(string text) => infoText.AddText(text, cp =>
            {
                cp.Font = cp.Font.With(weight: "Bold");
            });
        }

        private ArrayPool<Rgba32> createUploadPool()
        {
            // bucket size should be enough to allow some overhead when running multi-threaded with draw at 60hz.
            const int max_expected_thread_update_rate = 2000;

            int bucketSize = host.Threads.Count() * (max_expected_thread_update_rate / 60);

            return ArrayPool<Rgba32>.Create(FrameStatisticsDisplay.HEIGHT, bucketSize);
        }
    }

    public enum FrameStatisticsMode
    {
        None,
        Minimal,
        Full
    }
}
