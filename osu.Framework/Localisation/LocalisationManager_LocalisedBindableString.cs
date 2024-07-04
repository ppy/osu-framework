// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

#pragma warning disable 8632 // TODO: can be #nullable enable when Bindables are updated to also be.

using osu.Framework.Bindables;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private IBindable<LocalisationParameters> parameters;

            private LocalisableString text;

            private readonly LocalisationManager manager;

            public LocalisedBindableString(LocalisableString text, LocalisationManager manager)
            {
                this.text = text;
                this.manager = manager;

                updateValue();
            }

            private void updateValue()
            {
                Value = manager.GetLocalisedString(text);

                if (parameters == null && text.Data is ILocalisableStringData)
                {
                    parameters = new Bindable<LocalisationParameters>();
                    parameters.BindTo(manager.currentParameters);
                    parameters.BindValueChanged(_ => updateValue());
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

            internal override void UnbindAllInternal()
            {
                base.UnbindAllInternal();

                // optimisation to ensure cleanup happens aggressively.
                // without this, the central parameters bindable's internal WeakList can balloon out of control due to the
                // weak reference cleanup only occurring on Value retrieval (which rarely/never happens in this case).
                parameters?.UnbindAll();
            }
        }
    }
}
