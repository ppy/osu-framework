// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.IO.Stores;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly IBindable<IResourceStore<string>> storage = new Bindable<IResourceStore<string>>();

            private LocalisedString text;

            public LocalisedBindableString(IBindable<IResourceStore<string>> storage)
            {
                this.storage.BindTo(storage);
                this.storage.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                string newText = text.Text;

                if (text.ShouldLocalise && storage.Value != null)
                    newText = storage.Value.Get(newText);

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

            LocalisedString ILocalisedBindableString.Original
            {
                set
                {
                    if (text == value)
                        return;
                    text = value;

                    updateValue();
                }
            }
        }
    }
}
