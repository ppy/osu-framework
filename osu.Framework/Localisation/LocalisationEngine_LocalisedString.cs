// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;

namespace osu.Framework.Localisation
{
    public partial class LocalisationEngine
    {
        private class LocalisedString : Bindable<string>, ILocalisedString
        {
            public readonly IBindable<IResourceStore<string>> Storage = new Bindable<IResourceStore<string>>();

            private LocalisableString text;

            public LocalisedString()
            {
                Storage.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                if (Storage.Value == null)
                {
                    Value = text.Text;
                    return;
                }

                string newText = text.Text;

                if (text.Localised)
                    newText = Storage.Value.Get(newText);

                if (text.Args != null && !string.IsNullOrEmpty(newText))
                {
                    try
                    {
                        newText = string.Format(newText, text.Args);
                    }
                    catch (FormatException)
                    {
                        // Prevent crashes if the formatting fails. The string will be in a non-formatted state.
                    }
                }

                Value = newText;
            }

            LocalisableString ILocalisedString.Original
            {
                set
                {
                    text = value;
                    updateValue();
                }
            }
        }
    }
}
