﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using OpenTK;
using System.Threading.Tasks;
using osu.Framework.Caching;
using osu.Framework.Extensions.IEnumerableExtensions;
using OpenTK.Input;
using OpenTK.Graphics;

namespace osu.Framework.Platform
{
    public abstract class BasicGameHost : Container, IIpcHost
    {
        public BasicGameWindow Window;

        private void setActive(bool isActive)
        {
            threads.ForEach(t => t.IsActive = isActive);

            if (isActive)
                Activated?.Invoke();
            else
                Deactivated?.Invoke();
        }

        public bool IsActive => inputThread.IsActive;

        public bool IsPrimaryInstance { get; protected set; } = true;

        public event Action Activated;
        public event Action Deactivated;
        public event Func<bool> Exiting;
        public event Action Exited;

        public event Action<IpcMessage> MessageReceived;

        protected void OnMessageReceived(IpcMessage message) => MessageReceived?.Invoke(message);

        public virtual Task SendMessage(IpcMessage message)
        {
            throw new NotImplementedException("This platform does not implement IPC.");
        }

        public virtual BasicStorage Storage { get; protected set; } //public set currently required for visualtests setup.

        public override bool IsVisible => true;

        private GameThread[] threads;

        private static GameThread drawThread;
        private static GameThread updateThread;
        private static InputThread inputThread;

        private static Thread startupThread = Thread.CurrentThread;

        internal static Thread DrawThread => drawThread.Thread;
        internal static Thread UpdateThread => updateThread?.Thread.IsAlive ?? false ? updateThread.Thread : startupThread; //todo: check we still need this logic

        private double maximumUpdateHz;

        public double MaximumUpdateHz
        {
            get
            {
                return maximumUpdateHz;
            }

            set
            {
                updateThread.ActiveHz = maximumUpdateHz = value;
            }
        }

        private double maximumDrawHz;

        public double MaximumDrawHz
        {
            get
            {
                return maximumDrawHz;
            }

            set
            {
                drawThread.ActiveHz = maximumDrawHz = value;
            }
        }

        public double MaximumInactiveHz
        {
            get
            {
                return drawThread.InactiveHz;
            }

            set
            {
                drawThread.InactiveHz = value;
                updateThread.InactiveHz = value;
            }
        }

        protected internal PerformanceMonitor InputMonitor => inputThread.Monitor;
        protected internal PerformanceMonitor UpdateMonitor => updateThread.Monitor;
        protected internal PerformanceMonitor DrawMonitor => drawThread.Monitor;

        //null here to construct early but bind to thread late.
        public Scheduler InputScheduler => inputThread.Scheduler;
        protected Scheduler UpdateScheduler => updateThread.Scheduler;

        protected internal override IFrameBasedClock Clock => updateThread.Clock;

        private Cached<string> fullPathBacking = new Cached<string>();
        public string FullPath => fullPathBacking.EnsureValid() ? fullPathBacking.Value : fullPathBacking.Refresh(() =>
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Uri.UnescapeDataString(uri.Path);
        });

        private UserInputManager inputManager;

        protected override Container Content => inputManager;

        private string name;
        public override string Name => name;

        protected BasicGameHost(string gameName = @"")
        {
            name = gameName;

            threads = new[]
            {
                drawThread = new GameThread(DrawFrame, @"DrawThread")
                {
                    OnThreadStart = DrawInitialize,
                },
                updateThread = new GameThread(UpdateFrame, @"UpdateThread")
                {
                    OnThreadStart = UpdateInitialize,
                    Monitor = { HandleGC = true }
                },
                inputThread = new InputThread(null, @"MainThread") //never gets started.
            };

            MaximumUpdateHz = GameThread.DEFAULT_ACTIVE_HZ;
            MaximumDrawHz = (DisplayDevice.Default?.RefreshRate ?? 0) * 4;

            // Note, that RegisterCounters only has an effect for the first
            // BasicGameHost to be passed into it; i.e. the first BasicGameHost
            // to be instantiated.
            FrameStatistics.RegisterCounters(this);

            Environment.CurrentDirectory = Path.GetDirectoryName(FullPath);

            setActive(true);

            AddInternal(inputManager = new UserInputManager(this));
        }

        protected virtual void OnActivated() => Schedule(() => setActive(true));

        protected virtual void OnDeactivated() => Schedule(() => setActive(false));

        /// <returns>true to cancel</returns>
        protected virtual bool OnExitRequested()
        {
            if (ExitRequested) return false;

            bool? response = null;

            UpdateScheduler.Add(delegate
            {
                response = Exiting?.Invoke() == true;
            });

            //wait for a potentially blocking response
            while (!response.HasValue)
                Thread.Sleep(1);

            if (response.Value)
                return true;

            Exit();
            return false;
        }

        protected virtual void OnExited()
        {
            Exited?.Invoke();
        }

        protected TripleBuffer<DrawNode> DrawRoots = new TripleBuffer<DrawNode>();

        protected virtual void UpdateInitialize()
        {
            //this was added due to the dependency on GLWrapper.MaxTextureSize begin initialised.
            while (!GLWrapper.IsInitialized)
                Thread.Sleep(1);
        }

        protected void UpdateFrame()
        {
            using (UpdateMonitor.BeginCollecting(PerformanceCollectionType.Update))
            {
                UpdateSubTree();
                using (var buffer = DrawRoots.Get(UsageType.Write))
                    buffer.Object = GenerateDrawNodeSubtree(buffer.Index, ScreenSpaceDrawQuad.AABBf);
            }
        }

        protected virtual void DrawInitialize()
        {
            Window.MakeCurrent();
            GLWrapper.Initialize();

            if (Window != null)
                Window.VSync = VSyncMode.Off;
        }

        protected virtual void DrawFrame()
        {
            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.GLReset))
            {
                GLWrapper.Reset(DrawSize);
                GLWrapper.ClearColour(new Color4(0, 0, 0, 0));
            }

            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.Draw))
            {
                using (var buffer = DrawRoots.Get(UsageType.Read))
                    buffer?.Object?.Draw(null);

                GLWrapper.FlushCurrentBatch();
            }

            using (DrawMonitor.BeginCollecting(PerformanceCollectionType.SwapBuffer))
                Window.SwapBuffers();
        }

        protected bool ExitRequested;

        private bool threadsRunning => updateThread.Running || drawThread.Running;

        public void Exit()
        {
            InputScheduler.Add(delegate
            {
                ExitRequested = true;

                threads.ForEach(t => t.Exit());

                while (threadsRunning)
                    Thread.Sleep(1);
                Window?.Close();
            }, false);
        }

        public virtual void Run()
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            drawThread.Start();
            updateThread.Start();

            if (Window != null)
            {
                Window.KeyDown += window_KeyDown;
                Window.Resize += window_ClientSizeChanged;
                Window.ExitRequested += OnExitRequested;
                Window.Exited += OnExited;
                Window.Title = $@"osu.Framework (running ""{Name}"")";
                Window.FocusedChanged += delegate { setActive(Window.Focused); };
                window_ClientSizeChanged(null, null);

                try
                {
                    Window.UpdateFrame += delegate
                    {
                        inputPerformanceCollectionPeriod?.Dispose();
                        inputThread.RunUpdate();
                        inputPerformanceCollectionPeriod = InputMonitor.BeginCollecting(PerformanceCollectionType.WndProc);
                    };
                    Window.Run();
                }
                catch (OutOfMemoryException)
                {
                }
            }
            else
            {
                while (!ExitRequested)
                    inputThread.RunUpdate();
            }
        }

        private void window_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (!e.Control)
                return;
            switch (e.Key)
            {
                case Key.F7:
                    if (updateThread.ActiveHz == maximumUpdateHz)
                    {
                        updateThread.ActiveHz = double.MaxValue;
                        drawThread.ActiveHz = double.MaxValue;
                    }
                    else
                    {
                        updateThread.ActiveHz = maximumUpdateHz;
                        drawThread.ActiveHz = maximumDrawHz;
                    }
                    break;
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.WindowState == WindowState.Minimized) return;

            Rectangle rect = Window.ClientRectangle;
            UpdateScheduler.Add(delegate
            {
                //set base.Size here to avoid the override below, which would cause a recursive loop.
                base.Size = new Vector2(rect.Width, rect.Height);
            });
        }

        public override Vector2 Size
        {
            set
            {
                //this logic is shit, but necessary to make stuff not assert.
                //it's high priority to figure a better way to handle this, but i'm leaving it this way so we have a working codebase for now.
                UpdateScheduler.Add(delegate
                {
                    //update the underlying window size based on our new set size.
                    //important we do this before the base.Size set otherwise Invalidate logic will overwrite out new setting.
                    InputScheduler.Add(delegate { if (Window != null) Window.Size = new Size((int)value.X, (int)value.Y); });
                    base.Size = value;
                });
            }
        }

        InvokeOnDisposal inputPerformanceCollectionPeriod;

        public override void Add(Drawable drawable)
        {
            BaseGame game = drawable as BaseGame;
            Debug.Assert(game != null, @"Make sure to load a Game in a Host");

            game.SetHost(this);

            LoadGame(game);
        }

        protected virtual void LoadGame(BaseGame game)
        {
            // We are passing "null" as a parameter to Load to make sure BasicGameHost can never
            // depend on a Game object.
            if (!IsLoaded)
                Load(null);
            base.Add(game);
        }

        public abstract IEnumerable<InputHandler> GetInputHandlers();

        public abstract TextInputSource GetTextInput();
    }
}
