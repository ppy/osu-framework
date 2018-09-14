// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;

namespace osu.Framework.Localisation
{
    public partial class LocalisationEngine
    {
        private class LocalisedBindable : Bindable<string>
        {
            public readonly IBindable<IResourceStore<string>> Storage = new Bindable<IResourceStore<string>>();

            private readonly LocalisableString localisable;

            public LocalisedBindable(LocalisableString localisable)
                : base(localisable.Text)
            {
                this.localisable = localisable;

                Storage.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                if (Storage.Value == null)
                {
                    Value = localisable.Text;
                    return;
                }

                string newText = localisable.Text;

                if (localisable.Localised)
                    newText = Storage.Value.Get(newText);

                if (localisable.Args != null && !string.IsNullOrEmpty(newText))
                {
                    try
                    {
                        newText = string.Format(newText, localisable.Args);
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
