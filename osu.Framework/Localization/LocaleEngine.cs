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
            var bindable = new UnicodeBindableString(getUnicodePreference(unicode, nonUnicode));

            AddWeakReference(bindable);

            return bindable;
        }

        private string getUnicodePreference(string unicode, string nonUnicode) => preferUnicode ? unicode : nonUnicode;

        private void updateUnicodeStrings(bool newValue)
        {
            foreach (var w in unicodeBindings.ToArray())
            {
                UnicodeBindableString b;
                if (w.TryGetTarget(out b))
                    b.Value = getUnicodePreference(b.Unicode, b.NonUnicode);
                else
                    unicodeBindings.Remove(w);
            }
        }

        public class UnicodeBindableString : Bindable<string>
        {
            public string Unicode;
            public string NonUnicode;

            public UnicodeBindableString(string s) : base(s)
            {

            }
        }
    }
}
