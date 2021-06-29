// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#pragma warning disable 8632 // TODO: can be #nullable enable when Bindables are updated to also be.

using osu.Framework.Bindables;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly IBindable<ILocalisationStore?> storage = new Bindable<ILocalisationStore?>();

            // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable (reference must be kept for bindable to not get GC'd)
            private readonly IBindable<bool> preferUnicode;

            private LocalisableString text;
            private readonly FrameworkConfigManager config;

            public LocalisedBindableString(LocalisableString text, Bindable<ILocalisationStore?> storage, FrameworkConfigManager config)
            {
                this.text = text;

                this.storage.BindTo(storage);
                this.storage.BindValueChanged(_ => updateValue());

                this.config = config;

                preferUnicode = config.GetBindable<bool>(FrameworkSetting.ShowUnicode);
                preferUnicode.BindValueChanged(_ => updateValue());

                updateValue();
            }

            private void updateValue()
            {
                switch (text.Data)
                {
                    case string plain:
                        Value = plain;
                        break;

                    case ILocalisableStringData data:
                        Value = data.GetLocalised(storage.Value, config);
                        break;

                    default:
                        Value = string.Empty;
                        break;
                }
            }

            LocalisableString ILocalisedBindableString.Text
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
