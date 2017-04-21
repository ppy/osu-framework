// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Sprites
{
    public class LocalisedSpriteText : SpriteText
    {
        private Bindable<string> bindable;
        public Bindable<string> Bindable
        {
            get { return bindable; }
            set
            {
                if (bindable != null)
                    bindable.ValueChanged -= bindableChanged;
                if (value != null)
                {
                    value.ValueChanged += bindableChanged;
                    value.TriggerChange();
                }

                bindable = value;
            }
        }

        private void bindableChanged(string newValue)
        {
            Text = newValue;
        }
    }
}
