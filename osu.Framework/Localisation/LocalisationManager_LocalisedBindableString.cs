// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.IO.Stores;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly IBindable<IResourceStore<string>> storage = new Bindable<IResourceStore<string>>();
            private readonly IBindable<bool> preferUnicode = new Bindable<bool>();

            private LocalisableStringDescriptor text;

            public LocalisedBindableString(LocalisableStringDescriptor text, IBindable<IResourceStore<string>> storage, IBindable<bool> preferUnicode)
            {
                this.text = text;

                this.storage.BindTo(storage);
                this.preferUnicode.BindTo(preferUnicode);

                this.storage.BindValueChanged(_ => updateValue());
                this.preferUnicode.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                if (text.TryGetPlainText(out string plain))
                    Value = plain;
                else if (text.TryGetRomanization(out string romanized, out string unicode))
                {
                    Value = preferUnicode.Value && !string.IsNullOrEmpty(unicode) ? unicode : romanized;
                    if (string.IsNullOrEmpty(unicode))
                        Value = romanized;
                    else if (string.IsNullOrEmpty(romanized))
                        Value = unicode;
                    else
                        Value = preferUnicode.Value ? unicode : romanized;
                }
                else if (text.TryGetTranslatable(out string key, out string fallback, out object[] args))
                {
                    try
                    {
                        Value = string.Format(storage.Value.Get(key) ?? fallback, args);
                    }
                    catch (FormatException)
                    {
                        Value = fallback; // The formatting has failed.
                    }
                }
                else
                    Value = string.Empty;
            }

            LocalisableStringDescriptor ILocalisedBindableString.Text
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
