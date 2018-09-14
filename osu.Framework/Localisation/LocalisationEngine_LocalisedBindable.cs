// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public partial class LocalisationEngine
    {
        private class LocalisedBindable : Bindable<string>
        {
            public readonly Bindable<string> Locale = new Bindable<string>();

            private readonly LocalisableString localisable;
            private readonly LocalisationEngine engine;

            public LocalisedBindable(LocalisableString localisable, LocalisationEngine engine)
                : base(localisable.Text)
            {
                this.localisable = localisable;
                this.engine = engine;

                localisable.Text.BindValueChanged(_ => updateValue());
                localisable.Localised.BindValueChanged(_ => updateValue());
                localisable.Args.BindValueChanged(_ => updateValue());

                Locale.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                string newText = localisable.Text;

                if (localisable.Localised)
                    newText = engine.getLocalised(newText);

                if (localisable.Args.Value != null && !string.IsNullOrEmpty(newText))
                {
                    try
                    {
                        newText = string.Format(newText, localisable.Args.Value);
                    }
                    catch (FormatException)
                    {
                        // Prevent crashes if the formatting fails. The string will be in a non-formatted state.
                    }
                }

                Value = newText;
            }
        }
    }
}
