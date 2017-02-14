// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class ConfigManager<T> : IDisposable
        where T : struct
    {
        public virtual string Filename => @"game.ini";

        public virtual bool AddMissingEntries => true;

        bool hasUnsavedChanges;

        Dictionary<T, IBindable> configStore = new Dictionary<T, IBindable>();

        BasicStorage storage;

        public ConfigManager(BasicStorage storage)
        {
            this.storage = storage;
            InitialiseDefaults();
            Load();
        }

        protected virtual void InitialiseDefaults()
        {
        }

        public BindableDouble Set(T lookup, double value, double? min = null, double? max = null)
        {
            BindableDouble bindable = GetBindable<double>(lookup) as BindableDouble;

            if (bindable == null)
            {
                bindable = new BindableDouble(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            if (min.HasValue) bindable.MinValue = min.Value;
            if (max.HasValue) bindable.MaxValue = max.Value;

            return bindable;
        }

        public BindableInt Set(T lookup, int value, int? min = null, int? max = null)
        {
            BindableInt bindable = GetBindable<int>(lookup) as BindableInt;

            if (bindable == null)
            {
                bindable = new BindableInt(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            if (min.HasValue) bindable.MinValue = min.Value;
            if (max.HasValue) bindable.MaxValue = max.Value;

            return bindable;
        }

        public BindableBool Set(T lookup, bool value)
        {
            BindableBool bindable = GetBindable<bool>(lookup) as BindableBool;

            if (bindable == null)
            {
                bindable = new BindableBool(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            return bindable;
        }

        public Bindable<U> Set<U>(T lookup, U value)
        {
            Bindable<U> bindable = GetBindable<U>(lookup);

            if (bindable == null)
                bindable = set(lookup, value);
            else
                bindable.Value = value;

            return bindable;
        }

        private void addBindable(T lookup, IBindable bindable)
        {
            configStore[lookup] = bindable;
            bindable.ValueChanged += delegate { hasUnsavedChanges = true; };
        }

        private Bindable<U> set<U>(T lookup, U value)
        {
            Bindable<U> bindable = new Bindable<U>(value);
            addBindable(lookup, bindable);
            return bindable;
        }

        public U Get<U>(T lookup)
        {
            return GetBindable<U>(lookup).Value;
        }

        public Bindable<U> GetBindable<U>(T lookup)
        {
            IBindable obj;

            if (configStore.TryGetValue(lookup, out obj))
            {
                Bindable<U> bindable = obj as Bindable<U>;
                return bindable;
            }

            return set(lookup, default(U));
        }

        public void Load()
        {
            using (var stream = storage.GetStream(Filename))
            {
                if (stream == null)
                    return;

                string line;
                using (var reader = new StreamReader(stream))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        int equalsIndex = line.IndexOf('=');

                        if (line.Length == 0 || line[0] == '#' || equalsIndex < 0) continue;

                        string key = line.Substring(0, equalsIndex).Trim();
                        string val = line.Remove(0, equalsIndex + 1).Trim();

                        T lookup;

                        if (!Enum.TryParse(key, out lookup))
                            continue;

                        IBindable b;

                        if (configStore.TryGetValue(lookup, out b))
                            b.Parse(val);
                        else if (AddMissingEntries)
                            Set(lookup, val);
                    }
                }
            }
        }

        public bool Save()
        {
            if (!hasUnsavedChanges) return true;

            try
            {
                using (Stream stream = storage.GetStream(Filename, FileAccess.Write, FileMode.Create))
                using (StreamWriter w = new StreamWriter(stream))
                {
                    foreach (KeyValuePair<T, IBindable> p in configStore)
                        w.WriteLine(@"{0} = {1}", p.Key, p.Value);
                }
            }
            catch
            {
                return false;
            }

            hasUnsavedChanges = false;
            return true;
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Save();

                disposedValue = true;
            }
        }

        ~ConfigManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
