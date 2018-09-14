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
                localisable.Args.BindValueChanged(_ => updateValue());
                localisable.Type.BindValueChanged(_ => updateValue());

                Locale.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                string newText = localisable.Text;

                if ((localisable.Type & LocalisationType.Localised) > 0)
                    newText = engine.getLocalised(newText);

                if ((localisable.Type & LocalisationType.Formatted) > 0 && localisable.Args.Value != null && newText != null)
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
