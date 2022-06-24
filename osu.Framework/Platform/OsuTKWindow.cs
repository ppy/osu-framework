// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osuTK.Platform;
using osuTK.Input;
using System.ComponentModel;
using System.Drawing;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Threading;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Platform
{
    public abstract class OsuTKWindow : IWindow, IGameWindow
    {
        /// <summary>
        /// The <see cref="IGraphicsContext"/> associated with this <see cref="OsuTKWindow"/>.
        /// </summary>
        [NotNull]
        public abstract IGraphicsContext Context { get; }

        /// <summary>
        /// Invoked when the window close (X) button or another platform-native exit action has been pressed.
        /// </summary>
        public event Action ExitRequested;

        /// <summary>
        /// Invoked when the <see cref="OsuTKWindow"/> has closed.
        /// </summary>
        [CanBeNull]
        public event Action Exited;

        /// <summary>
        /// Invoked when the <see cref="OsuTKWindow"/> has resized.
        /// </summary>
        public event Action Resized;

        /// <inheritdoc cref="IWindow.KeymapChanged"/>
        public event Action KeymapChanged { add { } remove { } }

        /// <summary>
        /// Invoked when any key has been pressed.
        /// </summary>
        [CanBeNull]
        public event EventHandler<KeyboardKeyEventArgs> KeyDown;

        internal readonly Version GLVersion;
        internal readonly Version GLSLVersion;
        internal readonly bool IsEmbedded;

        protected readonly IGameWindow OsuTKGameWindow;

        protected readonly Scheduler UpdateFrameScheduler = new Scheduler();

        private readonly BindableBool cursorInWindow = new BindableBool(true);

        public IBindable<bool> CursorInWindow => cursorInWindow;

        /// <summary>
        /// Available resolutions for full-screen display.
        /// </summary>
        public virtual IEnumerable<DisplayResolution> AvailableResolutions => Enumerable.Empty<DisplayResolution>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        public abstract bool Focused { get; }

        public abstract IBindable<bool> IsActive { get; }

        public virtual IEnumerable<Display> Displays => new[] { DisplayDevice.GetDisplay(DisplayIndex.Primary).ToDisplay() };

        public virtual Display PrimaryDisplay => Displays.FirstOrDefault(d => d.Index == (int)DisplayDevice.Default.GetIndex());

        public Bindable<Display> CurrentDisplayBindable { get; } = new Bindable<Display>();

        /// <summary>
        /// osuTK's reference to the current <see cref="DisplayResolution"/> instance is private.
        /// Instead we construct a <see cref="DisplayMode"/> based on the metrics of <see cref="CurrentDisplayBindable"/>,
        /// as it defers to the current resolution. Note that we round the refresh rate, as osuTK can sometimes
        /// report refresh rates such as 59.992863 where SDL2 will report 60.
        /// </summary>
        public virtual IBindable<DisplayMode> CurrentDisplayMode
        {
            get
            {
                var display = CurrentDisplayDevice;
                return new Bindable<DisplayMode>(new DisplayMode(null, new Size(display.Width, display.Height), display.BitsPerPixel, (int)Math.Round(display.RefreshRate), 0));
            }
        }

        /// <summary>
        /// Creates a <see cref="OsuTKWindow"/> with a given <see cref="IGameWindow"/> implementation.
        /// </summary>
        protected OsuTKWindow([NotNull] IGameWindow osuTKGameWindow)
        {
            OsuTKGameWindow = osuTKGameWindow;
            OsuTKGameWindow.KeyDown += OnKeyDown;

            CurrentDisplayBindable.Value = PrimaryDisplay;

            // Moving or resizing the window needs to check to see if we've moved to a different display.
            // This will update the CurrentDisplay bindable.
            Move += (_, _) => checkCurrentDisplay();
            Resize += (_, _) =>
            {
                checkCurrentDisplay();
                Resized?.Invoke();
            };

            Closing += (_, e) =>
            {
                // always block a graceful exit as it's treated as a regular window event.
                // the host will force-close the window if the game decides not to block the exit.
                ExitRequested?.Invoke();
                e.Cancel = true;
            };
            Closed += (_, _) => Exited?.Invoke();

            MouseEnter += (_, _) => cursorInWindow.Value = true;
            MouseLeave += (_, _) => cursorInWindow.Value = false;

            supportedWindowModes.AddRange(DefaultSupportedWindowModes);

            UpdateFrame += (_, _) => UpdateFrameScheduler.Update();

            MakeCurrent();

            string version = GL.GetString(StringName.Version);
            string versionNumberSubstring = getVersionNumberSubstring(version);

            GLVersion = new Version(versionNumberSubstring);

            // As defined by https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glGetString.xml
            IsEmbedded = version.Contains("OpenGL ES");

            version = GL.GetString(StringName.ShadingLanguageVersion);

            if (!string.IsNullOrEmpty(version))
            {
                try
                {
                    GLSLVersion = new Version(versionNumberSubstring);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $@"couldn't set GLSL version using string '{version}'");
                }
            }

            if (GLSLVersion == null)
                GLSLVersion = new Version();

            Logger.Log($@"GL Initialized
                        GL Version:                 {GL.GetString(StringName.Version)}
                        GL Renderer:                {GL.GetString(StringName.Renderer)}
                        GL Shader Language version: {GL.GetString(StringName.ShadingLanguageVersion)}
                        GL Vendor:                  {GL.GetString(StringName.Vendor)}
                        GL Extensions:              {GL.GetString(StringName.Extensions)}");
        }

        /// <summary>
        /// Creates a <see cref="OsuTKWindow"/> with given dimensions.
        /// <para>Note that this will use the default <see cref="GameWindow"/> implementation, which is not compatible with every platform.</para>
        /// </summary>
        protected OsuTKWindow(int width, int height)
            : this(new GameWindow(width, height, new GraphicsMode(GraphicsMode.Default.ColorFormat, GraphicsMode.Default.Depth, GraphicsMode.Default.Stencil, GraphicsMode.Default.Samples, GraphicsMode.Default.AccumulatorFormat, 3)))
        {
        }

        public void Create()
        {
            Context.MakeCurrent(null);
        }

        private CursorState cursorState = CursorState.Default;

        /// <summary>
        /// Controls the state of the OS cursor.
        /// </summary>
        public virtual CursorState CursorState
        {
            get => cursorState;
            set
            {
                cursorState = value;

                OsuTKGameWindow.Cursor = cursorState.HasFlagFast(CursorState.Hidden) ? MouseCursor.Empty : MouseCursor.Default;

                try
                {
                    OsuTKGameWindow.CursorGrabbed = cursorState.HasFlagFast(CursorState.Confined);
                }
                catch
                {
                    // may not be supported by platform.
                }
            }
        }

        public RectangleF? CursorConfineRect { get; set; }

        /// <summary>
        /// We do not support directly using <see cref="Cursor"/>.
        /// It is controlled internally. Use <see cref="CursorState"/> instead.
        /// </summary>
        public MouseCursor Cursor
        {
            get => throw new InvalidOperationException($@"{nameof(Cursor)} is not supported. Use {nameof(CursorState)}.");
            set => throw new InvalidOperationException($@"{nameof(Cursor)} is not supported. Use {nameof(CursorState)}.");
        }

        /// <summary>
        /// We do not support directly using <see cref="CursorVisible"/>.
        /// It is controlled internally. Use <see cref="CursorState"/> instead.
        /// </summary>
        public bool CursorVisible
        {
            get => throw new InvalidOperationException($@"{nameof(CursorVisible)} is not supported. Use {nameof(CursorState)}.");
            set => throw new InvalidOperationException($@"{nameof(CursorVisible)} is not supported. Use {nameof(CursorState)}.");
        }

        /// <summary>
        /// We do not support directly using <see cref="CursorGrabbed"/>.
        /// It is controlled internally. Use <see cref="CursorState"/> instead.
        /// </summary>
        public bool CursorGrabbed
        {
            get => throw new InvalidOperationException($@"{nameof(CursorGrabbed)} is not supported. Use {nameof(CursorState)}.");
            set => throw new InvalidOperationException($@"{nameof(CursorGrabbed)} is not supported. Use {nameof(CursorState)}.");
        }

        /// <summary>
        /// Gets the <see cref="DisplayDevice"/> that this window is currently on.
        /// </summary>
        protected virtual DisplayDevice CurrentDisplayDevice
        {
            get => DisplayDevice.FromRectangle(Bounds) ?? DisplayDevice.Default;
            set => throw new InvalidOperationException($@"{GetType().Name}.{nameof(CurrentDisplayDevice)} cannot be set.");
        }

        private void checkCurrentDisplay()
        {
            int index = (int)CurrentDisplayDevice.GetIndex();
            if (index != CurrentDisplayBindable.Value?.Index)
                CurrentDisplayBindable.Value = Displays.ElementAtOrDefault(index);
        }

        private string getVersionNumberSubstring(string version)
        {
            string result = version.Split(' ').FirstOrDefault(s => char.IsDigit(s, 0));
            if (result != null) return result;

            throw new ArgumentException($"Cannot get version number from {version}!", nameof(version));
        }

        public abstract void SetupWindow(FrameworkConfigManager config);

        protected virtual void OnKeyDown(object sender, KeyboardKeyEventArgs e) => KeyDown?.Invoke(sender, e);

        /// <summary>
        /// Provides a <see cref="BindableMarginPadding"/> that can be used to keep track of the "safe area" insets on mobile
        /// devices.  This usually corresponds to areas of the screen hidden under notches and rounded corners.
        /// The safe area insets are provided by the operating system and dynamically change as the user rotates the device.
        /// </summary>
        public virtual BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        private readonly BindableList<WindowMode> supportedWindowModes = new BindableList<WindowMode>();

        public IBindableList<WindowMode> SupportedWindowModes => supportedWindowModes;

        public virtual WindowMode DefaultWindowMode => SupportedWindowModes.First();

        protected abstract IEnumerable<WindowMode> DefaultSupportedWindowModes { get; }

        public virtual VSyncMode VSync { get; set; }

        public bool VerticalSync
        {
            get => VSync == VSyncMode.On;
            set => VSync = value ? VSyncMode.On : VSyncMode.Off;
        }

        public virtual void CycleMode()
        {
            var currentValue = WindowMode.Value;

            do
            {
                switch (currentValue)
                {
                    case Configuration.WindowMode.Windowed:
                        currentValue = Configuration.WindowMode.Borderless;
                        break;

                    case Configuration.WindowMode.Borderless:
                        currentValue = Configuration.WindowMode.Fullscreen;
                        break;

                    case Configuration.WindowMode.Fullscreen:
                        currentValue = Configuration.WindowMode.Windowed;
                        break;
                }
            } while (!SupportedWindowModes.Contains(currentValue) && currentValue != WindowMode.Value);

            WindowMode.Value = currentValue;
        }

        public void ClearCurrent() => GraphicsContext.CurrentContext?.MakeCurrent(null);

        #region Autogenerated IGameWindow implementation

        public virtual void Run() => OsuTKGameWindow.Run();

        public void Run(double updateRate)
        {
            OsuTKGameWindow.Run(updateRate);
        }

        public void MakeCurrent() => OsuTKGameWindow.MakeCurrent();
        public void SwapBuffers() => OsuTKGameWindow.SwapBuffers();

        public string Title
        {
            get => OsuTKGameWindow.Title;
            set => OsuTKGameWindow.Title = $"{value} (legacy osuTK)";
        }

        bool INativeWindow.Focused => OsuTKGameWindow.Focused;

        public bool Visible
        {
            get => OsuTKGameWindow.Visible;
            set => OsuTKGameWindow.Visible = value;
        }

        public bool Exists => OsuTKGameWindow.Exists;
        public IWindowInfo WindowInfo => OsuTKGameWindow.WindowInfo;

        osuTK.WindowState INativeWindow.WindowState
        {
            get => OsuTKGameWindow.WindowState;
            set => OsuTKGameWindow.WindowState = value;
        }

        public virtual WindowState WindowState
        {
            get => OsuTKGameWindow.WindowState.ToFramework();
            set => OsuTKGameWindow.WindowState = value.ToOsuTK();
        }

        public WindowBorder WindowBorder
        {
            get => OsuTKGameWindow.WindowBorder;
            set => OsuTKGameWindow.WindowBorder = value;
        }

        public Rectangle Bounds
        {
            get => OsuTKGameWindow.Bounds;
            set => OsuTKGameWindow.Bounds = value;
        }

        public Point Location
        {
            get => OsuTKGameWindow.Location;
            set => OsuTKGameWindow.Location = value;
        }

        public Size Size
        {
            get => OsuTKGameWindow.Size;
            set => OsuTKGameWindow.Size = value;
        }

        public int X
        {
            get => OsuTKGameWindow.X;
            set => OsuTKGameWindow.X = value;
        }

        public int Y
        {
            get => OsuTKGameWindow.Y;
            set => OsuTKGameWindow.Y = value;
        }

        public int Width
        {
            get => OsuTKGameWindow.Width;
            set => OsuTKGameWindow.Width = value;
        }

        public int Height
        {
            get => OsuTKGameWindow.Height;
            set => OsuTKGameWindow.Height = value;
        }

        public Rectangle ClientRectangle
        {
            get => OsuTKGameWindow.ClientRectangle;
            set => OsuTKGameWindow.ClientRectangle = value;
        }

        public Size ClientSize
        {
            get => OsuTKGameWindow.ClientSize;
            set => OsuTKGameWindow.ClientSize = value;
        }

        public Size MinSize
        {
            get => throw new InvalidOperationException($@"{nameof(MinSize)} is not supported.");
            set => throw new InvalidOperationException($@"{nameof(MinSize)} is not supported.");
        }

        public Size MaxSize
        {
            get => throw new InvalidOperationException($@"{nameof(MaxSize)} is not supported.");
            set => throw new InvalidOperationException($@"{nameof(MaxSize)} is not supported.");
        }

        public void Close() => OsuTKGameWindow.Close();

        public void ProcessEvents() => OsuTKGameWindow.ProcessEvents();
        public Point PointToClient(Point point) => OsuTKGameWindow.PointToClient(point);
        public Point PointToScreen(Point point) => OsuTKGameWindow.PointToScreen(point);

        public Icon Icon
        {
            get => OsuTKGameWindow.Icon;
            set => OsuTKGameWindow.Icon = value;
        }

        public void Dispose() => OsuTKGameWindow.Dispose();

        public event EventHandler<EventArgs> Load
        {
            add => OsuTKGameWindow.Load += value;
            remove => OsuTKGameWindow.Load -= value;
        }

        public event EventHandler<EventArgs> Unload
        {
            add => OsuTKGameWindow.Unload += value;
            remove => OsuTKGameWindow.Unload -= value;
        }

        public event EventHandler<FrameEventArgs> UpdateFrame
        {
            add => OsuTKGameWindow.UpdateFrame += value;
            remove => OsuTKGameWindow.UpdateFrame -= value;
        }

        public event EventHandler<FrameEventArgs> RenderFrame
        {
            add => OsuTKGameWindow.RenderFrame += value;
            remove => OsuTKGameWindow.RenderFrame -= value;
        }

        public event EventHandler<EventArgs> Move
        {
            add => OsuTKGameWindow.Move += value;
            remove => OsuTKGameWindow.Move -= value;
        }

        public event EventHandler<EventArgs> Resize
        {
            add => OsuTKGameWindow.Resize += value;
            remove => OsuTKGameWindow.Resize -= value;
        }

        public event EventHandler<CancelEventArgs> Closing
        {
            add => OsuTKGameWindow.Closing += value;
            remove => OsuTKGameWindow.Closing -= value;
        }

        public event EventHandler<EventArgs> Closed
        {
            add => OsuTKGameWindow.Closed += value;
            remove => OsuTKGameWindow.Closed -= value;
        }

        public event EventHandler<EventArgs> Disposed
        {
            add => OsuTKGameWindow.Disposed += value;
            remove => OsuTKGameWindow.Disposed -= value;
        }

        public event EventHandler<EventArgs> IconChanged
        {
            add => OsuTKGameWindow.IconChanged += value;
            remove => OsuTKGameWindow.IconChanged -= value;
        }

        public event EventHandler<EventArgs> TitleChanged
        {
            add => OsuTKGameWindow.TitleChanged += value;
            remove => OsuTKGameWindow.TitleChanged -= value;
        }

        public event EventHandler<EventArgs> VisibleChanged
        {
            add => OsuTKGameWindow.VisibleChanged += value;
            remove => OsuTKGameWindow.VisibleChanged -= value;
        }

        public event EventHandler<EventArgs> FocusedChanged
        {
            add => OsuTKGameWindow.FocusedChanged += value;
            remove => OsuTKGameWindow.FocusedChanged -= value;
        }

        public event EventHandler<EventArgs> WindowBorderChanged
        {
            add => OsuTKGameWindow.WindowBorderChanged += value;
            remove => OsuTKGameWindow.WindowBorderChanged -= value;
        }

        public event EventHandler<EventArgs> WindowStateChanged
        {
            add => OsuTKGameWindow.WindowStateChanged += value;
            remove => OsuTKGameWindow.WindowStateChanged -= value;
        }

        public event EventHandler<KeyPressEventArgs> KeyPress
        {
            add => OsuTKGameWindow.KeyPress += value;
            remove => OsuTKGameWindow.KeyPress -= value;
        }

        public event EventHandler<KeyboardKeyEventArgs> KeyUp
        {
            add => OsuTKGameWindow.KeyUp += value;
            remove => OsuTKGameWindow.KeyUp -= value;
        }

        public event EventHandler<EventArgs> MouseLeave
        {
            add => OsuTKGameWindow.MouseLeave += value;
            remove => OsuTKGameWindow.MouseLeave -= value;
        }

        public event EventHandler<EventArgs> MouseEnter
        {
            add => OsuTKGameWindow.MouseEnter += value;
            remove => OsuTKGameWindow.MouseEnter -= value;
        }

        public event EventHandler<MouseButtonEventArgs> MouseDown
        {
            add => OsuTKGameWindow.MouseDown += value;
            remove => OsuTKGameWindow.MouseDown -= value;
        }

        public event EventHandler<MouseButtonEventArgs> MouseUp
        {
            add => OsuTKGameWindow.MouseUp += value;
            remove => OsuTKGameWindow.MouseUp -= value;
        }

        public event EventHandler<MouseMoveEventArgs> MouseMove
        {
            add => OsuTKGameWindow.MouseMove += value;
            remove => OsuTKGameWindow.MouseMove -= value;
        }

        public event EventHandler<MouseWheelEventArgs> MouseWheel
        {
            add => OsuTKGameWindow.MouseWheel += value;
            remove => OsuTKGameWindow.MouseWheel -= value;
        }

        public event EventHandler<FileDropEventArgs> FileDrop
        {
            add => OsuTKGameWindow.FileDrop += value;
            remove => OsuTKGameWindow.FileDrop -= value;
        }

        #endregion
    }

    /// <summary>
    /// Describes our supported states of the OS cursor.
    /// </summary>
    [Flags]
    public enum CursorState
    {
        /// <summary>
        /// The OS cursor is always visible and can move anywhere.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The OS cursor is hidden while hovering the <see cref="OsuTKWindow"/>, but can still move anywhere.
        /// </summary>
        Hidden = 1,

        /// <summary>
        /// The OS cursor is confined to the <see cref="OsuTKWindow"/> while the window is in focus.
        /// </summary>
        Confined = 2,

        /// <summary>
        /// The OS cursor is hidden while hovering the <see cref="OsuTKWindow"/>.
        /// It is confined to the <see cref="OsuTKWindow"/> while the window is in focus and can move freely otherwise.
        /// </summary>
        HiddenAndConfined = Hidden | Confined,
    }
}
