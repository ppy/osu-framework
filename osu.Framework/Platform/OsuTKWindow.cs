﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
using osu.Framework.Threading;
using Icon = osuTK.Icon;

namespace osu.Framework.Platform
{
    public abstract class OsuTKWindow : IWindow
    {
        /// <summary>
        /// The <see cref="IGraphicsContext"/> associated with this <see cref="OsuTKWindow"/>.
        /// </summary>
        [NotNull]
        public abstract IGraphicsContext Context { get; }

        /// <summary>
        /// Return value decides whether we should intercept and cancel this exit (if possible).
        /// </summary>
        [CanBeNull]
        public event Func<bool> ExitRequested;

        /// <summary>
        /// Invoked when the <see cref="OsuTKWindow"/> has closed.
        /// </summary>
        [CanBeNull]
        public event Action Exited;

        /// <summary>
        /// Invoked when any key has been pressed.
        /// </summary>
        [CanBeNull]
        public event EventHandler<KeyboardKeyEventArgs> KeyDown;

        internal readonly Version GLVersion;
        internal readonly Version GLSLVersion;
        internal readonly bool IsEmbedded;

        protected readonly IGameWindow Implementation;

        protected readonly Scheduler UpdateFrameScheduler = new Scheduler();

        private readonly BindableBool cursorInWindow = new BindableBool(true);

        public IBindable<bool> CursorInWindow => cursorInWindow;

        /// <summary>
        /// Available resolutions for full-screen display.
        /// </summary>
        public virtual IEnumerable<DisplayResolution> AvailableResolutions => Enumerable.Empty<DisplayResolution>();

        public Bindable<WindowMode> WindowMode { get; } = new Bindable<WindowMode>();

        private readonly Bindable<bool> isActive = new Bindable<bool>();

        /// <summary>
        /// Whether this <see cref="OsuTKWindow"/> is active (in the foreground).
        /// </summary>
        public IBindable<bool> IsActive => isActive;

        public virtual IEnumerable<Display> Displays => new[] { DisplayDevice.GetDisplay(DisplayIndex.Primary).ToDisplay() };

        public virtual Display PrimaryDisplay => Displays.FirstOrDefault(d => d.Index == (int)DisplayDevice.Default.GetIndex());

        public virtual Bindable<Display> CurrentDisplay { get; } = new Bindable<Display>();

        /// <summary>
        /// osuTK's reference to the current <see cref="DisplayResolution"/> instance is private.
        /// Instead we construct a <see cref="DisplayMode"/> based on the metrics of <see cref="CurrentDisplay"/>,
        /// as it defers to the current resolution. Note that we round the refresh rate, as osuTK can sometimes
        /// report refresh rates such as 59.992863 where SDL2 will report 60.
        /// </summary>
        public virtual DisplayMode CurrentDisplayMode
        {
            get
            {
                var display = CurrentDisplayDevice;
                return new DisplayMode(null, new Size(display.Width, display.Height), display.BitsPerPixel, (int)Math.Round(display.RefreshRate), 0, 0);
            }
        }

        /// <summary>
        /// Creates a <see cref="OsuTKWindow"/> with a given <see cref="IGameWindow"/> implementation.
        /// </summary>
        protected OsuTKWindow([NotNull] IGameWindow implementation)
        {
            Implementation = implementation;
            Implementation.KeyDown += OnKeyDown;

            CurrentDisplay.Value = PrimaryDisplay;

            // Moving or resizing the window needs to check to see if we've moved to a different display.
            // This will update the CurrentDisplay bindable.
            Move += (sender, e) => checkCurrentDisplay();
            Resize += (sender, e) => checkCurrentDisplay();

            Closing += (sender, e) => e.Cancel = ExitRequested?.Invoke() ?? false;
            Closed += (sender, e) => Exited?.Invoke();

            MouseEnter += (sender, args) => cursorInWindow.Value = true;
            MouseLeave += (sender, args) => cursorInWindow.Value = false;

            FocusedChanged += (o, e) => isActive.Value = Focused;

            supportedWindowModes.AddRange(DefaultSupportedWindowModes);

            UpdateFrame += (o, e) => UpdateFrameScheduler.Update();
            UpdateFrameScheduler.Add(() => isActive.Value = Focused);

            WindowStateChanged += (o, e) => isActive.Value = WindowState != osuTK.WindowState.Minimized;

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

            Context.MakeCurrent(null);
        }

        /// <summary>
        /// Creates a <see cref="OsuTKWindow"/> with given dimensions.
        /// <para>Note that this will use the default <see cref="osuTK.GameWindow"/> implementation, which is not compatible with every platform.</para>
        /// </summary>
        protected OsuTKWindow(int width, int height)
            : this(new GameWindow(width, height, new GraphicsMode(GraphicsMode.Default.ColorFormat, GraphicsMode.Default.Depth, GraphicsMode.Default.Stencil, GraphicsMode.Default.Samples, GraphicsMode.Default.AccumulatorFormat, 3)))
        {
        }

        /// <summary>
        /// Not applicable to osuTK.
        /// </summary>
        public void Create()
        {
        }

        private CursorState cursorState = CursorState.Default;

        /// <summary>
        /// Controls the state of the OS cursor.
        /// </summary>
        public CursorState CursorState
        {
            get => cursorState;
            set
            {
                cursorState = value;

                Implementation.Cursor = cursorState.HasFlag(CursorState.Hidden) ? MouseCursor.Empty : MouseCursor.Default;

                try
                {
                    Implementation.CursorGrabbed = cursorState.HasFlag(CursorState.Confined);
                }
                catch
                {
                    // may not be supported by platform.
                }
            }
        }

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
            if (index != CurrentDisplay.Value?.Index)
                CurrentDisplay.Value = Displays.ElementAtOrDefault(index);
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

        void IWindow.MakeCurrent() => ((IGameWindow)this).MakeCurrent();

        public void ClearCurrent() => GraphicsContext.CurrentContext?.MakeCurrent(null);

        #region Autogenerated IGameWindow implementation

        public virtual void Run() => Implementation.Run();
        public virtual void Run(double updateRate) => Implementation.Run(updateRate);
        public void MakeCurrent() => Implementation.MakeCurrent();
        public void SwapBuffers() => Implementation.SwapBuffers();

        Icon INativeWindow.Icon
        {
            get => Implementation.Icon;
            set => Implementation.Icon = value;
        }

        public string Title
        {
            get => Implementation.Title;
            set => Implementation.Title = value;
        }

        public virtual bool Focused => Implementation.Focused;

        public bool Visible
        {
            get => Implementation.Visible;
            set => Implementation.Visible = value;
        }

        public bool Exists => Implementation.Exists;
        public IWindowInfo WindowInfo => Implementation.WindowInfo;

        public virtual osuTK.WindowState WindowState
        {
            get => Implementation.WindowState;
            set => Implementation.WindowState = value;
        }

        public WindowBorder WindowBorder
        {
            get => Implementation.WindowBorder;
            set => Implementation.WindowBorder = value;
        }

        public Rectangle Bounds
        {
            get => Implementation.Bounds;
            set => Implementation.Bounds = value;
        }

        public Point Location
        {
            get => Implementation.Location;
            set => Implementation.Location = value;
        }

        public Size Size
        {
            get => Implementation.Size;
            set => Implementation.Size = value;
        }

        public int X
        {
            get => Implementation.X;
            set => Implementation.X = value;
        }

        public int Y
        {
            get => Implementation.Y;
            set => Implementation.Y = value;
        }

        public int Width
        {
            get => Implementation.Width;
            set => Implementation.Width = value;
        }

        public int Height
        {
            get => Implementation.Height;
            set => Implementation.Height = value;
        }

        public Rectangle ClientRectangle
        {
            get => Implementation.ClientRectangle;
            set => Implementation.ClientRectangle = value;
        }

        public Size ClientSize
        {
            get => Implementation.ClientSize;
            set => Implementation.ClientSize = value;
        }

        public void Close() => Implementation.Close();
        public void ProcessEvents() => Implementation.ProcessEvents();
        public Point PointToClient(Point point) => Implementation.PointToClient(point);
        public Point PointToScreen(Point point) => Implementation.PointToScreen(point);
        public void Dispose() => Implementation.Dispose();

        public event EventHandler<EventArgs> Load
        {
            add => Implementation.Load += value;
            remove => Implementation.Load -= value;
        }

        public event EventHandler<EventArgs> Unload
        {
            add => Implementation.Unload += value;
            remove => Implementation.Unload -= value;
        }

        public event EventHandler<FrameEventArgs> UpdateFrame
        {
            add => Implementation.UpdateFrame += value;
            remove => Implementation.UpdateFrame -= value;
        }

        public event EventHandler<FrameEventArgs> RenderFrame
        {
            add => Implementation.RenderFrame += value;
            remove => Implementation.RenderFrame -= value;
        }

        public event EventHandler<EventArgs> Move
        {
            add => Implementation.Move += value;
            remove => Implementation.Move -= value;
        }

        public event EventHandler<EventArgs> Resize
        {
            add => Implementation.Resize += value;
            remove => Implementation.Resize -= value;
        }

        public event EventHandler<CancelEventArgs> Closing
        {
            add => Implementation.Closing += value;
            remove => Implementation.Closing -= value;
        }

        public event EventHandler<EventArgs> Closed
        {
            add => Implementation.Closed += value;
            remove => Implementation.Closed -= value;
        }

        public event EventHandler<EventArgs> Disposed
        {
            add => Implementation.Disposed += value;
            remove => Implementation.Disposed -= value;
        }

        public event EventHandler<EventArgs> IconChanged
        {
            add => Implementation.IconChanged += value;
            remove => Implementation.IconChanged -= value;
        }

        public event EventHandler<EventArgs> TitleChanged
        {
            add => Implementation.TitleChanged += value;
            remove => Implementation.TitleChanged -= value;
        }

        public event EventHandler<EventArgs> VisibleChanged
        {
            add => Implementation.VisibleChanged += value;
            remove => Implementation.VisibleChanged -= value;
        }

        public event EventHandler<EventArgs> FocusedChanged
        {
            add => Implementation.FocusedChanged += value;
            remove => Implementation.FocusedChanged -= value;
        }

        public event EventHandler<EventArgs> WindowBorderChanged
        {
            add => Implementation.WindowBorderChanged += value;
            remove => Implementation.WindowBorderChanged -= value;
        }

        public event EventHandler<EventArgs> WindowStateChanged
        {
            add => Implementation.WindowStateChanged += value;
            remove => Implementation.WindowStateChanged -= value;
        }

        public event EventHandler<KeyPressEventArgs> KeyPress
        {
            add => Implementation.KeyPress += value;
            remove => Implementation.KeyPress -= value;
        }

        public event EventHandler<KeyboardKeyEventArgs> KeyUp
        {
            add => Implementation.KeyUp += value;
            remove => Implementation.KeyUp -= value;
        }

        public event EventHandler<EventArgs> MouseLeave
        {
            add => Implementation.MouseLeave += value;
            remove => Implementation.MouseLeave -= value;
        }

        public event EventHandler<EventArgs> MouseEnter
        {
            add => Implementation.MouseEnter += value;
            remove => Implementation.MouseEnter -= value;
        }

        public event EventHandler<MouseButtonEventArgs> MouseDown
        {
            add => Implementation.MouseDown += value;
            remove => Implementation.MouseDown -= value;
        }

        public event EventHandler<MouseButtonEventArgs> MouseUp
        {
            add => Implementation.MouseUp += value;
            remove => Implementation.MouseUp -= value;
        }

        public event EventHandler<MouseMoveEventArgs> MouseMove
        {
            add => Implementation.MouseMove += value;
            remove => Implementation.MouseMove -= value;
        }

        public event EventHandler<MouseWheelEventArgs> MouseWheel
        {
            add => Implementation.MouseWheel += value;
            remove => Implementation.MouseWheel -= value;
        }

        public event EventHandler<FileDropEventArgs> FileDrop
        {
            add => Implementation.FileDrop += value;
            remove => Implementation.FileDrop -= value;
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
