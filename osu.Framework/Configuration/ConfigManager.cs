// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Configuration.Tracking;

namespace osu.Framework.Configuration
{
    public abstract class ConfigManager<T> : ITrackableConfigManager, IDisposable
        where T : struct
    {
        protected virtual bool AddMissingEntries => true;

        protected readonly Dictionary<T, IBindable> ConfigStore = new Dictionary<T, IBindable>();

        protected virtual void InitialiseDefaults()
        {
        }

        public BindableDouble Set(T lookup, double value, double? min = null, double? max = null, double? precision = null)
        {
            if (!(GetOriginalBindable<double>(lookup) is BindableDouble bindable))
            {
                bindable = new BindableDouble(value);
                AddBindable(lookup, bindable);
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
            if (!(GetOriginalBindable<float>(lookup) is BindableFloat bindable))
            {
                bindable = new BindableFloat(value);
                AddBindable(lookup, bindable);
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
            if (!(GetOriginalBindable<int>(lookup) is BindableInt bindable))
            {
                bindable = new BindableInt(value);
                AddBindable(lookup, bindable);
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
            if (!(GetOriginalBindable<bool>(lookup) is BindableBool bindable))
            {
                bindable = new BindableBool(value);
                AddBindable(lookup, bindable);
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

        protected virtual void AddBindable<TBindable>(T lookup, Bindable<TBindable> bindable)
        {
            ConfigStore[lookup] = bindable;
            bindable.ValueChanged += _ => backgroundSave();
        }

        private Bindable<U> set<U>(T lookup, U value)
        {
            Bindable<U> bindable = new Bindable<U>(value);
            AddBindable(lookup, bindable);
            return bindable;
        }

        public U Get<U>(T lookup) => GetOriginalBindable<U>(lookup).Value;

        protected Bindable<U> GetOriginalBindable<U>(T lookup)
        {
            if (ConfigStore.TryGetValue(lookup, out IBindable obj))
                return obj as Bindable<U>;

            return null;
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

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                Save();
                isDisposed = true;
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

        public virtual TrackedSettings CreateTrackedSettings() => null;

        public void LoadInto(TrackedSettings settings) => settings.LoadFrom(this);

        public class TrackedSetting<U> : Tracking.TrackedSetting<U>
        {
            /// <summary>
            /// Constructs a new <see cref="TrackedSetting{U}"/>.
            /// </summary>
            /// <param name="setting">The config setting to be tracked.</param>
            /// <param name="generateDescription">A function that generates the description for the setting, invoked every time the value changes.</param>
            public TrackedSetting(T setting, Func<U, SettingDescription> generateDescription)
                : base(setting, generateDescription)
            {
            }
        }

        private bool hasLoaded;

        public void Load()
        {
            PerformLoad();
            hasLoaded = true;
        }

        private int lastSave;

        /// <summary>
        /// Perform a save with debounce.
        /// </summary>
        private void backgroundSave()
        {
            var current = Interlocked.Increment(ref lastSave);
            Task.Delay(100).ContinueWith(task =>
            {
                if (current == lastSave) Save();
            });
        }

        private readonly object saveLock = new object();

        public bool Save()
        {
            if (!hasLoaded) return false;

            lock (saveLock)
            {
                Interlocked.Increment(ref lastSave);
                return PerformSave();
            }
        }

        protected abstract void PerformLoad();

        protected abstract bool PerformSave();
    }
}
