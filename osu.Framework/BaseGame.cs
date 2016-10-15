﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Windows.Forms;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Performance;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;
using OpenTK;
using FlowDirection = osu.Framework.Graphics.Containers.FlowDirection;

namespace osu.Framework
{
    public class BaseGame : Container
    {
        public BasicGameWindow Window => host?.Window;

        public ResourceStore<byte[]> Resources;

        public TextureStore Textures;

        /// <summary>
        /// This should point to the main resource dll file. If not specified, it will use resources embedded in your executable.
        /// </summary>
        protected virtual string MainResourceFile => Host.FullPath;

        private BasicGameForm form => host?.Window?.Form;
        private BasicGameHost host;

        public BasicGameHost Host => host;

        private bool isActive;

        public AudioManager Audio;

        public ShaderManager Shaders;

        public TextureStore Fonts;

        private Container content;
        private FlowContainer performanceContainer;

        public bool ShowPerformanceOverlay
        {
            get { return performanceContainer.Alpha > 0; }
            set { performanceContainer.FadeTo(value ? 1 : 0, 200); }
        }

        protected override Container Content => content;

        public BaseGame()
        {
            RelativeSizeAxes = Axes.Both;

            AddInternal(new Drawable[]
            {
                performanceContainer = new PerformanceOverlay
                {
                    Position = new Vector2(5, 5),
                    Direction = FlowDirection.VerticalOnly,
                    Alpha = 0,
                    Spacing = new Vector2(10, 10),
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Depth = float.MaxValue
                },
                content = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                }
            });
        }

        /// <summary>
        /// As Load is run post host creation, you can override this method to alter properties of the host before it makes itself visible to the user.
        /// </summary>
        /// <param name="host"></param>
        public virtual void SetHost(BasicGameHost host)
        {
            this.host = host;
            host.Exiting += OnExiting;

            if (form != null)
            {
                form.FormClosing += OnFormClosing;
                form.DragEnter += dragEnter;
                form.DragDrop += dragDrop;
            }
        }

        public override void Load(BaseGame game)
        {
            Resources = new ResourceStore<byte[]>();
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));
            Resources.AddStore(new DllResourceStore(MainResourceFile));

            Textures = new TextureStore(new RawTextureLoaderStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures")));
            Textures.AddStore(new RawTextureLoaderStore(new OnlineStore()));

            Audio = new AudioManager(new NamespacedResourceStore<byte[]>(Resources, @"Tracks"), new NamespacedResourceStore<byte[]>(Resources, @"Samples"));

            Shaders = new ShaderManager(new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));

            Fonts = new TextureStore(new GlyphStore(Resources, @"Fonts/OpenSans"))
            {
                ScaleAdjust = 1 / 100f
            };

            base.Load(game);
        }

        protected override void Update()
        {
            Audio.Update();
            base.Update();
        }

        private void dragDrop(object sender, DragEventArgs e)
        {
            Array fileDrop = e.Data.GetData(DataFormats.FileDrop) as Array;
            string textDrop = e.Data.GetData(DataFormats.Text) as string;

            if (fileDrop != null)
            {
                for (int i = 0; i < fileDrop.Length; i++)
                    OnDroppedFile(fileDrop.GetValue(i).ToString());
            }

            if (!string.IsNullOrEmpty(textDrop))
                OnDroppedText(textDrop);
        }

        private void dragEnter(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
            bool isUrl = e.Data.GetDataPresent(DataFormats.Text);
            e.Effect = isFile || isUrl ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        /// Whether the Game environment is active (in the foreground).
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            private set
            {
                if (value == isActive)
                    return;
                isActive = value;

                if (isActive)
                    OnActivated();
                else
                    OnDeactivated();
            }
        }

        public void Exit()
        {
            host.Exit();
        }

        protected virtual void OnDroppedText(string text)
        {
        }

        protected virtual void OnDroppedFile(string file)
        {
        }

        protected virtual void OnFormClosing(object sender, FormClosingEventArgs args)
        {
        }

        protected virtual void OnDragEnter(object sender, EventArgs args)
        {
        }

        protected virtual void OnActivated()
        {
        }

        protected virtual void OnDeactivated()
        {
        }

        protected virtual bool OnExiting()
        {
            return false;
        }

        /// <summary>
        /// Called before a frame cycle has started (Update and Draw).
        /// </summary>
        protected virtual void PreFrame()
        {
        }

        /// <summary>
        /// Called after a frame cycle has been completed (Update and Draw).
        /// </summary>
        protected virtual void PostFrame()
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Audio?.Dispose();
            Audio = null;
        }
    }
}
