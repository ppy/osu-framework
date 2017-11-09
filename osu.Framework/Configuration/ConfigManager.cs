// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Logging;
using osu.Framework.Platform;
using System;
using System.Collections.Generic;
using System.IO;

namespace osu.Framework.Configuration
{
    public class ConfigManager<T> : IDisposable
        where T : struct
    {
        /// <summary>
        /// The backing file used to store the config. Null means no persistent storage.
        /// </summary>
        protected virtual string Filename => @"game.ini";

        protected virtual bool AddMissingEntries => true;

        private bool hasUnsavedChanges;

        private readonly Dictionary<T, IBindable> configStore = new Dictionary<T, IBindable>();

        private readonly Storage storage;

        public ConfigManager(Storage storage)
        {
            this.storage = storage;
            InitialiseDefaults();
            Load();
        }

        protected virtual void InitialiseDefaults()
        {
        }

        public BindableDouble Set(T lookup, double value, double? min = null, double? max = null, double? precision = null)
        {
            BindableDouble bindable = GetOriginalBindable<double>(lookup) as BindableDouble;

            if (bindable == null)
            {
                bindable = new BindableDouble(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            bindable.Default = value;
            if (min.HasValue) bindable.MinValue = min.Value;
            if (max.HasValue) bindable.MaxValue = max.Value;
            if (precision.HasValue) bindable.Precision = precision.Value;

            return bindable;
        }

        public BindableFloat Set(T lookup, float value, float? min = null, float? max = null, float? precision = null)
        {
            BindableFloat bindable = GetOriginalBindable<float>(lookup) as BindableFloat;

            if (bindable == null)
            {
                bindable = new BindableFloat(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            bindable.Default = value;
            if (min.HasValue) bindable.MinValue = min.Value;
            if (max.HasValue) bindable.MaxValue = max.Value;
            if (precision.HasValue) bindable.Precision = precision.Value;

            return bindable;
        }

        public BindableInt Set(T lookup, int value, int? min = null, int? max = null)
        {
            BindableInt bindable = GetOriginalBindable<int>(lookup) as BindableInt;

            if (bindable == null)
            {
                bindable = new BindableInt(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            bindable.Default = value;
            if (min.HasValue) bindable.MinValue = min.Value;
            if (max.HasValue) bindable.MaxValue = max.Value;

            return bindable;
        }

        public BindableBool Set(T lookup, bool value)
        {
            BindableBool bindable = GetOriginalBindable<bool>(lookup) as BindableBool;

            if (bindable == null)
            {
                bindable = new BindableBool(value);
                addBindable(lookup, bindable);
            }
            else
            {
                bindable.Value = value;
            }

            bindable.Default = value;

            return bindable;
        }

        public Bindable<U> Set<U>(T lookup, U value)
        {
            Bindable<U> bindable = GetOriginalBindable<U>(lookup);

            if (bindable == null)
                bindable = set(lookup, value);
            else
                bindable.Value = value;

            bindable.Default = value;

            return bindable;
        }

        private void addBindable<TBindable>(T lookup, Bindable<TBindable> bindable)
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
            return GetOriginalBindable<U>(lookup).Value;
        }

        protected Bindable<U> GetOriginalBindable<U>(T lookup)
        {
            IBindable obj;

            if (configStore.TryGetValue(lookup, out obj))
                return obj as Bindable<U>;

            return set(lookup, default(U));
        }

        /// <summary>
        /// Retrieve a bindable. This will be a new instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        public Bindable<U> GetBindable<U>(T lookup) => GetOriginalBindable<U>(lookup)?.GetBoundCopy();

        /// <summary>
        /// Binds a local bindable with a configuration-backed bindable.
        /// </summary>
        public void BindWith<U>(T lookup, Bindable<U> bindable) => bindable.BindTo(GetOriginalBindable<U>(lookup));

        public void Load()
        {
            if (string.IsNullOrEmpty(Filename)) return;

            using (var stream = storage.GetStream(Filename))
            {
                if (stream == null)
                    return;

                using (var reader = new StreamReader(stream))
                {
                    string line;

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
                        {
                            try
                            {
                                b.Parse(val);
                            }
                            catch (Exception e)
                            {
                                Logger.Log($@"Unable to parse config key {lookup}: {e}", LoggingTarget.Runtime, LogLevel.Important);
                            }
                        }
                        else if (AddMissingEntries)
                            Set(lookup, val);
                    }
                }
            }
        }

        public bool Save()
        {
            if (!hasUnsavedChanges || string.IsNullOrEmpty(Filename)) return true;

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
