// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using System;
using System.Collections.Generic;

namespace osu.Framework.Localization
{
    public class LocalisationEngine
    {
        private Bindable<bool> preferUnicode;

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            preferUnicode = config.GetBindable<bool>(FrameworkConfig.ShowUnicode);
            preferUnicode.ValueChanged += updateUnicodeStrings;
        }

        private List<WeakReference<UnicodeBindableString>> unicodeBindings = new List<WeakReference<UnicodeBindableString>>();

        protected void AddWeakReference(UnicodeBindableString unicodeBindable) => unicodeBindings.Add(new WeakReference<UnicodeBindableString>(unicodeBindable));

        public UnicodeBindableString GetUnicodePreference(string unicode, string nonUnicode)
        {
            var bindable = new UnicodeBindableString(unicode, nonUnicode)
            {
                PreferUnicode = preferUnicode.Value
            };
            AddWeakReference(bindable);

            return bindable;
        }

        private void updateUnicodeStrings(bool newValue)
        {
            foreach (var w in unicodeBindings.ToArray())
            {
                UnicodeBindableString b;
                if (w.TryGetTarget(out b))
                    b.PreferUnicode = newValue;
                else
                    unicodeBindings.Remove(w);
            }
        }

        public class UnicodeBindableString : Bindable<string>
        {
            public readonly string Unicode;
            public readonly string NonUnicode;

            public UnicodeBindableString(string unicode, string nonUnicode) : base(nonUnicode)
            {
                Unicode = unicode;
                NonUnicode = nonUnicode;
            }

            public bool PreferUnicode
            {
                get { return Value == Unicode; }
                set { Value = value ? Unicode : NonUnicode; }
            }
        }
    }
}
