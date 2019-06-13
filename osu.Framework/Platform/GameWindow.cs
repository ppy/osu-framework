// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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
using osu.Framework.Graphics;
using Icon = osuTK.Icon;

namespace osu.Framework.Platform
{
    public abstract class GameWindow : BaseWindow
    {
        /// <summary>
        /// The <see cref="IGraphicsContext"/> associated with this <see cref="GameWindow"/>.
        /// </summary>
        [NotNull]
        public abstract IGraphicsContext Context { get; }

        /// <summary>
        /// Invoked when any key has been pressed.
        /// </summary>
        [CanBeNull]
        public override event EventHandler<KeyboardKeyEventArgs> KeyDown;

        internal readonly Version GLVersion;
        internal readonly Version GLSLVersion;
        internal readonly bool IsEmbedded;

        protected readonly IGameWindow Implementation;

        private readonly Bindable<bool> isActive = new Bindable<bool>();

        /// <summary>
        /// Whether this <see cref="GameWindow"/> is active (in the foreground).
        /// </summary>
        public override IBindable<bool> IsActive => isActive;

        /// <summary>
        /// Creates a <see cref="GameWindow"/> with a given <see cref="IGameWindow"/> implementation.
        /// </summary>
        protected GameWindow([NotNull] IGameWindow implementation)
        {
            Implementation = implementation;
            Implementation.KeyDown += OnKeyDown;

            Closing += (sender, e) => e.Cancel = OnExitRequested();
            Closed += (sender, e) => OnExited();

            MouseEnter += (sender, args) => CursorInWindow = true;
            MouseLeave += (sender, args) => CursorInWindow = false;

            FocusedChanged += (o, e) => isActive.Value = Focused;

            supportedWindowModes.AddRange(DefaultSupportedWindowModes);

            bool firstUpdate = true;
            UpdateFrame += (o, e) =>
            {
                if (firstUpdate)
                {
                    isActive.Value = Focused;
                    firstUpdate = false;
                }
            };

            WindowStateChanged += (o, e) => isActive.Value = WindowState != WindowState.Minimized;

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
        /// Creates a <see cref="GameWindow"/> with given dimensions.
        /// <para>Note that this will use the default <see cref="osuTK.GameWindow"/> implementation, which is not compatible with every platform.</para>
        /// </summary>
        protected GameWindow(int width, int height)
            : this(new osuTK.GameWindow(width, height, new GraphicsMode(GraphicsMode.Default.ColorFormat, GraphicsMode.Default.Depth, GraphicsMode.Default.Stencil, GraphicsMode.Default.Samples, GraphicsMode.Default.AccumulatorFormat, 3)))
        {
        }

        private CursorState cursorState = CursorState.Default;

        public override CursorState CursorState
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
        /// Gets the <see cref="DisplayDevice"/> that this window is currently on.
        /// </summary>
        /// <returns></returns>
        public override DisplayDevice CurrentDisplay
        {
            get => DisplayDevice.FromRectangle(Bounds) ?? DisplayDevice.Default;
            protected set => throw new InvalidOperationException($@"{GetType().Name}.{nameof(CurrentDisplay)} cannot be set.");
        }

        private string getVersionNumberSubstring(string version)
        {
            string result = version.Split(' ').FirstOrDefault(s => char.IsDigit(s, 0));
            if (result != null) return result;

            throw new ArgumentException(nameof(version));
        }

        protected virtual void OnKeyDown(object sender, KeyboardKeyEventArgs e) => KeyDown?.Invoke(sender, e);

        private readonly BindableList<WindowMode> supportedWindowModes = new BindableList<WindowMode>();

        public override IBindableList<WindowMode> SupportedWindowModes => supportedWindowModes;

        public override WindowMode DefaultWindowMode => SupportedWindowModes.First();

        protected abstract IEnumerable<WindowMode> DefaultSupportedWindowModes { get; }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            Implementation.Dispose();
        }

        #region Autogenerated IGameWindow implementation

        public override void Run() => Implementation.Run();
        public override void Run(double updateRate) => Implementation.Run(updateRate);
        public override void MakeCurrent() => Implementation.MakeCurrent();
        public override void SwapBuffers() => Implementation.SwapBuffers();

        public override Icon Icon
        {
            get => Implementation.Icon;
            set => Implementation.Icon = value;
        }

        public override string Title
        {
            get => Implementation.Title;
            set => Implementation.Title = value;
        }

        public override bool Focused => Implementation.Focused;

        public override bool Visible
        {
            get => Implementation.Visible;
            set => Implementation.Visible = value;
        }

        public override bool Exists => Implementation.Exists;
        public override IWindowInfo WindowInfo => Implementation.WindowInfo;

        public override WindowState WindowState
        {
            get => Implementation.WindowState;
            set => Implementation.WindowState = value;
        }

        public override WindowBorder WindowBorder
        {
            get => Implementation.WindowBorder;
            set => Implementation.WindowBorder = value;
        }

        public override Rectangle Bounds
        {
            get => Implementation.Bounds;
            set => Implementation.Bounds = value;
        }

        public override Point Location
        {
            get => Implementation.Location;
            set => Implementation.Location = value;
        }

        public override Size Size
        {
            get => Implementation.Size;
            set => Implementation.Size = value;
        }

        public override int X
        {
            get => Implementation.X;
            set => Implementation.X = value;
        }

        public override int Y
        {
            get => Implementation.Y;
            set => Implementation.Y = value;
        }

        public override int Width
        {
            get => Implementation.Width;
            set => Implementation.Width = value;
        }

        public override int Height
        {
            get => Implementation.Height;
            set => Implementation.Height = value;
        }

        public override Rectangle ClientRectangle
        {
            get => Implementation.ClientRectangle;
            set => Implementation.ClientRectangle = value;
        }

        public override Size ClientSize
        {
            get => Implementation.ClientSize;
            set => Implementation.ClientSize = value;
        }

        public override void Close() => Implementation.Close();
        public override void ProcessEvents() => Implementation.ProcessEvents();
        public override Point PointToClient(Point point) => Implementation.PointToClient(point);
        public override Point PointToScreen(Point point) => Implementation.PointToScreen(point);

        public override event EventHandler<EventArgs> Load
        {
            add => Implementation.Load += value;
            remove => Implementation.Load -= value;
        }

        public override event EventHandler<EventArgs> Unload
        {
            add => Implementation.Unload += value;
            remove => Implementation.Unload -= value;
        }

        public override event EventHandler<FrameEventArgs> UpdateFrame
        {
            add => Implementation.UpdateFrame += value;
            remove => Implementation.UpdateFrame -= value;
        }

        public override event EventHandler<FrameEventArgs> RenderFrame
        {
            add => Implementation.RenderFrame += value;
            remove => Implementation.RenderFrame -= value;
        }

        public override event EventHandler<EventArgs> Move
        {
            add => Implementation.Move += value;
            remove => Implementation.Move -= value;
        }

        public override event EventHandler<EventArgs> Resize
        {
            add => Implementation.Resize += value;
            remove => Implementation.Resize -= value;
        }

        public override event EventHandler<CancelEventArgs> Closing
        {
            add => Implementation.Closing += value;
            remove => Implementation.Closing -= value;
        }

        public override event EventHandler<EventArgs> Closed
        {
            add => Implementation.Closed += value;
            remove => Implementation.Closed -= value;
        }

        public override event EventHandler<EventArgs> Disposed
        {
            add => Implementation.Disposed += value;
            remove => Implementation.Disposed -= value;
        }

        public override event EventHandler<EventArgs> IconChanged
        {
            add => Implementation.IconChanged += value;
            remove => Implementation.IconChanged -= value;
        }

        public override event EventHandler<EventArgs> TitleChanged
        {
            add => Implementation.TitleChanged += value;
            remove => Implementation.TitleChanged -= value;
        }

        public override event EventHandler<EventArgs> VisibleChanged
        {
            add => Implementation.VisibleChanged += value;
            remove => Implementation.VisibleChanged -= value;
        }

        public override event EventHandler<EventArgs> FocusedChanged
        {
            add => Implementation.FocusedChanged += value;
            remove => Implementation.FocusedChanged -= value;
        }

        public override event EventHandler<EventArgs> WindowBorderChanged
        {
            add => Implementation.WindowBorderChanged += value;
            remove => Implementation.WindowBorderChanged -= value;
        }

        public override event EventHandler<EventArgs> WindowStateChanged
        {
            add => Implementation.WindowStateChanged += value;
            remove => Implementation.WindowStateChanged -= value;
        }

        public override event EventHandler<KeyPressEventArgs> KeyPress
        {
            add => Implementation.KeyPress += value;
            remove => Implementation.KeyPress -= value;
        }

        public override event EventHandler<KeyboardKeyEventArgs> KeyUp
        {
            add => Implementation.KeyUp += value;
            remove => Implementation.KeyUp -= value;
        }

        public override event EventHandler<EventArgs> MouseLeave
        {
            add => Implementation.MouseLeave += value;
            remove => Implementation.MouseLeave -= value;
        }

        public override event EventHandler<EventArgs> MouseEnter
        {
            add => Implementation.MouseEnter += value;
            remove => Implementation.MouseEnter -= value;
        }

        public override event EventHandler<MouseButtonEventArgs> MouseDown
        {
            add => Implementation.MouseDown += value;
            remove => Implementation.MouseDown -= value;
        }

        public override event EventHandler<MouseButtonEventArgs> MouseUp
        {
            add => Implementation.MouseUp += value;
            remove => Implementation.MouseUp -= value;
        }

        public override event EventHandler<MouseMoveEventArgs> MouseMove
        {
            add => Implementation.MouseMove += value;
            remove => Implementation.MouseMove -= value;
        }

        public override event EventHandler<MouseWheelEventArgs> MouseWheel
        {
            add => Implementation.MouseWheel += value;
            remove => Implementation.MouseWheel -= value;
        }

        public override event EventHandler<FileDropEventArgs> FileDrop
        {
            add => Implementation.FileDrop += value;
            remove => Implementation.FileDrop -= value;
        }

        #endregion
    }
}
