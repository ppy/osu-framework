// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using System.Text.RegularExpressions;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using System;
using OpenTK;

namespace osu.Framework.Graphics.Sprites
{
    public class Paragraph : FillFlowContainer
    {
        private int headerIndent;
        public int HeaderIndent
        {
            get { return headerIndent; }
            set
            {
                if (value == headerIndent) return;
                headerIndent = value;

                realignText();
            }
        }

        private int bodyIndent;
        public int BodyIndent
        {
            get { return bodyIndent; }
            set
            {
                if (value == bodyIndent) return;
                bodyIndent = value;

                realignText();
            }
        }

        private float textSize = 20f;
        public float TextSize
        {
            get { return textSize; }
            set
            {
                if (textSize == value) return;
                textSize = value;

				recreateText();
            }
        }

        private string font = @"";
        public string Font
        {
            get { return font; }
            set
            {
                if (font == value) return;
                font = value;

                recreateText();
            }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                if (text == value) return;
                text = value;

                recreateText();
            }
        }

        public override bool HandleInput => false;

        private float lastWidth;
        protected override void UpdateAfterChildren()
        {
        	base.UpdateAfterChildren();

        	//partially broken, not properly aligned for most when only running once a resize
        	// todo: fix this ^
        	if (lastWidth == DrawWidth || BodyIndent <= 0 && HeaderIndent <= 0) return;
        	realignText();
        	lastWidth = DrawWidth;
        }

        public void AddText(string text, Action<SpriteText> onCreate)
        {
            var sprites = spritesFromText(text, (t) => new SpriteText
            {
                Text = t,
                TextSize = TextSize,
                Font = Font,
            });

            Add(sprites);

            foreach (Drawable d in sprites)
            {
                if (d is SpriteText)
                    onCreate?.Invoke(d as SpriteText);
            }
        }

        private void recreateText()
        {
            Children = spritesFromText(Text, (t) => new SpriteText
            {
                Text = t,
                TextSize = TextSize,
                Font = Font,
            });
        }

        private IEnumerable<Drawable> spritesFromText(string text, Func<string, SpriteText> create)
        {
        	List<Drawable> sprites = new List<Drawable>();
        	foreach (string l in Regex.Split(text, @"[\n]"))
        	{
        		//newlines
        		sprites.Add(new Container
        		{
        			RelativeSizeAxes = Axes.X,
        			Height = sprites.LastOrDefault() is Container ? TextSize : 0f,
        		});

        		foreach (string w in Regex.Split(l, @"(?<=[ .,;-])")) //split at wrapping chars
        		{
        			if (w != @"")
        			{
        				sprites.Add(create.Invoke(w));
        			}
        		}
        	}

        	return sprites;
        }

        private void realignText()
        {
            List<Drawable> children = Children.ToList();

            for (int i = 0; i < children.Count; i++)
            {
                children[i].Margin = new MarginPadding
                {
                    Left = children[i].Position.X > 0 || children[i] is Container ? 0 : children.ElementAtOrDefault(i - 1) is Container ? HeaderIndent : BodyIndent,
                };
            }
        }
    }
}
