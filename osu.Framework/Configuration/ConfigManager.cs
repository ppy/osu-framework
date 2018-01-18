// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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

        /// <summary>
        /// Retrieves all the settings of this <see cref="ConfigManager{T}"/> that are to be tracked for changes.
        /// </summary>
        /// <returns>A list of <see cref="TrackedSetting"/>.</returns>
        public virtual TrackedSettings CreateTrackedSettings() => null;

        public class TrackedSettings : List<TrackedSetting>, ITrackedSettings
        {
            public event Action<SettingDescription> SettingChanged;

            /// <summary>
            /// Begins tracking all the contained settings.
            /// </summary>
            /// <param name="configManager">The <see cref="ConfigManager{T}"/> to track from.</param>
            public void LoadFrom(ConfigManager<T> configManager)
            {
                foreach (var value in this)
                {
                    value.LoadFrom(configManager);
                    value.SettingChanged = d => SettingChanged?.Invoke(d);
                }
            }

            public void Unload()
            {
                foreach (var value in this)
                    value.Unload();
            }
        }

        public abstract class TrackedSetting
        {
            /// <summary>
            /// Invoked when this setting has changed.
            /// </summary>
            internal Action<SettingDescription> SettingChanged;

            internal abstract void LoadFrom(ConfigManager<T> configManager);
            internal abstract void Unload();
        }

        public class TrackedSetting<U> : TrackedSetting
        {
            private readonly T setting;
            private readonly Func<U, SettingDescription> generateDescription;

            private Bindable<U> bindable;

            public TrackedSetting(T setting, Func<U, SettingDescription> generateDescription)
            {
                this.setting = setting;
                this.generateDescription = generateDescription;
            }

            internal override void LoadFrom(ConfigManager<T> configManager)
            {
                bindable = configManager.GetBindable<U>(setting);
                bindable.ValueChanged += displaySetting;
            }

            internal override void Unload()
            {
                bindable.ValueChanged -= displaySetting;
                bindable = null;
            }

            private void displaySetting(U value) => SettingChanged?.Invoke(generateDescription(value));
        }
    }

    public interface ITrackedSettings
    {
        /// <summary>
        /// Invoked when the value of any tracked setting has changed.
        /// </summary>
        event Action<SettingDescription> SettingChanged;

        /// <summary>
        /// Stops tracking all contained settings.
        /// </summary>
        void Unload();
    }

    /// <summary>
    /// Contains information that may be displayed when tracked settings change.
    /// </summary>
    public class SettingDescription
    {
        /// <summary>
        /// The raw setting value.
        /// </summary>
        public readonly object RawValue;

        /// <summary>
        /// The readable setting name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The readable setting value.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// The shortcut keys that cause this setting to change.
        /// </summary>
        public readonly string Shortcut;

        /// <summary>
        /// Constructs a new <see cref="SettingDescription"/>.
        /// </summary>
        /// <param name="rawValue">The raw setting value.</param>
        /// <param name="name">The readable setting name.</param>
        /// <param name="value">The readable setting value.</param>
        /// <param name="shortcut">The shortcut keys that cause this setting to change.</param>
        public SettingDescription(object rawValue, string name, string value, string shortcut = @"")
        {
            RawValue = rawValue;
            Name = name;
            Value = value;
            Shortcut = shortcut;
        }
    }
}
