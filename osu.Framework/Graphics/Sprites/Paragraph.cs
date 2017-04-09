// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using System.Text.RegularExpressions;
using System.Linq;

namespace osu.Framework.Graphics.Sprites
{
    public class Paragraph : FillFlowContainer
    {
        private string text;
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                if (text == value) return;
                text = value;

                List<Drawable> sprites = new List<Drawable>();
                foreach (string l in Regex.Split(value, @"[\n]"))
                {
                    //newlines
                    sprites.Add(new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = sprites.LastOrDefault() is Container ? 20f : 0f,
                    });

                    foreach (string w in Regex.Split(l, @"(?<=[ .,;-])")) //split at wrapping chars
                    {
                        if (w != @"")
                        {
                            sprites.Add(new SpriteText
                            {
                                Text = w,
                            });
                        }
                    }
                }

                Children = sprites;
            }
        }

        public override bool HandleInput => false;
    }
}
