// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using osu.Framework;
using osu.Framework.Graphics;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;
using osu.Framework.Platform;
using OpenTK.Input;

namespace SampleGame {
    internal class SampleGame : Game {
        private Box box;

        [BackgroundDependencyLoader]
        private void load() {
            Add(box = new Box {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });
            //((DesktopGameWindow)Window).Position = new Vector2(0.5f, 0.5f);
        }

        protected override void Update() {
            base.Update();
            box.Rotation += (float)Time.Elapsed / 10;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) {
            var win = (DesktopGameWindow)Window;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Bounds:   " + win.Bounds);
            Console.WriteLine("Client:   " + win.ClientSize);
            Console.WriteLine("Location: " + win.Location);
            Console.WriteLine("Size:     " + win.Size);
            Console.WriteLine("Mode:     " + win.WindowMode);
            Console.WriteLine("State:    " + win.WindowState);
            Console.ResetColor();

            switch(args.Button) {

                case MouseButton.Left:
                    win.WindowMode.Value = win.WindowMode.Value == WindowMode.Borderless ? WindowMode.Windowed : WindowMode.Borderless;
                    break;

                case MouseButton.Right:
                    win.WindowMode.Value = WindowMode.Windowed;
                    win.ClientSize = new Size(1280, 720);
                    win.Position = new Vector2(1, 1);
                    break;
            }
            return true;
        }
    }
}
