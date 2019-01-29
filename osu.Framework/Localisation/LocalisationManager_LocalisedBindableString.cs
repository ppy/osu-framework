// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            private readonly IBindable<bool> preferUnicode = new Bindable<bool>();

            private LocalisedString text;

            public LocalisedBindableString(LocalisedString text, IBindable<IResourceStore<string>> storage, IBindable<bool> preferUnicode)
            {
                this.text = text;

                this.storage.BindTo(storage);
                this.preferUnicode.BindTo(preferUnicode);

                this.storage.BindValueChanged(_ => updateValue());
                this.preferUnicode.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                string newText = preferUnicode.Value ? text.Text.Original : text.Text.Fallback;

                if (text.ShouldLocalise && storage.Value != null)
                    newText = storage.Value.Get(newText);

                if (text.Args?.Length > 0 && !string.IsNullOrEmpty(newText))
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

            LocalisedString ILocalisedBindableString.Text
            {
                set
                {
                    if (text.Equals(value))
                        return;
                    text = value;

                    updateValue();
                }
            }
        }
    }
}
