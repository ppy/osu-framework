// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.Configuration.Tracking;

namespace osu.Framework.Configuration
{
    public abstract class ConfigManager<TLookup> : ConfigManager, ITrackableConfigManager
        where TLookup : struct, Enum
    {
        /// <summary>
        /// Whether user specified configuration elements should be set even though a default was never specified.
        /// </summary>
        protected virtual bool AddMissingEntries => true;

        private readonly IDictionary<TLookup, object> defaultOverrides;

        protected readonly Dictionary<TLookup, IBindable> ConfigStore = new Dictionary<TLookup, IBindable>();

        /// <summary>
        /// Initialise a new <see cref="ConfigManager{TLookup}"/>
        /// </summary>
        /// <param name="defaultOverrides">Dictionary of overrides which should take precedence over defaults specified by the <see cref="ConfigManager{TLookup}"/> implementation.</param>
        protected ConfigManager(IDictionary<TLookup, object> defaultOverrides = null)
        {
            this.defaultOverrides = defaultOverrides;
        }

        /// <summary>
        /// Get the full configuration for logging purposes.
        /// </summary>
        /// <remarks>
        /// Excludes any potentially sensitive information via <see cref="CheckLookupContainsPrivateInformation"/>.</remarks>
        /// <returns></returns>
        public virtual IDictionary<TLookup, string> GetCurrentConfigurationForLogging() => ConfigStore
                                                                                           .Where(kvp => !CheckLookupContainsPrivateInformation(kvp.Key))
                                                                                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        /// <summary>
        /// Check whether a specific lookup may contain private user information.
        /// </summary>
        /// <param name="lookup">The lookup type to check.</param>
        /// <returns>Whether private information is present.</returns>
        protected virtual bool CheckLookupContainsPrivateInformation(TLookup lookup) => false;

        /// <summary>
        /// Set all required default values via Set() calls.
        /// Note that defaults set here may be overridden by <see cref="defaultOverrides"/> provided in the constructor.
        /// </summary>
        protected virtual void InitialiseDefaults()
        {
        }

        /// <summary>
        /// Sets a configuration's value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The value. Will also become the default value if one has not already been initialised.</param>
        /// <typeparam name="TValue">The type of value.</typeparam>
        public void SetValue<TValue>(TLookup lookup, TValue value)
        {
            var bindable = GetOriginalBindable<TValue>(lookup);

            if (bindable == null)
                SetDefault(lookup, value);
            else
                bindable.Value = value;
        }

        /// <summary>
        /// Sets a configuration's default value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The default value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="precision">The value precision.</param>
        /// <returns>The original bindable (not a bound copy).</returns>
        protected BindableDouble SetDefault(TLookup lookup, double value, double? min = null, double? max = null, double? precision = null)
        {
            value = getDefault(lookup, value);

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

        /// <summary>
        /// Sets a configuration's default value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The default value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="precision">The value precision.</param>
        /// <returns>The original bindable (not a bound copy).</returns>
        protected BindableFloat SetDefault(TLookup lookup, float value, float? min = null, float? max = null, float? precision = null)
        {
            value = getDefault(lookup, value);

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

        /// <summary>
        /// Sets a configuration's default value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The default value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The original bindable (not a bound copy).</returns>
        protected BindableInt SetDefault(TLookup lookup, int value, int? min = null, int? max = null)
        {
            value = getDefault(lookup, value);

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

        /// <summary>
        /// Sets a configuration's default value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The default value.</param>
        /// <returns>The original bindable (not a bound copy).</returns>
        protected BindableBool SetDefault(TLookup lookup, bool value)
        {
            value = getDefault(lookup, value);

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

        /// <summary>
        /// Sets a configuration's default value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The default value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The original bindable (not a bound copy).</returns>
        protected BindableSize SetDefault(TLookup lookup, Size value, Size? min = null, Size? max = null)
        {
            value = getDefault(lookup, value);

            if (!(GetOriginalBindable<Size>(lookup) is BindableSize bindable))
            {
                bindable = new BindableSize(value);
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

        /// <summary>
        /// Sets a configuration's default value.
        /// </summary>
        /// <param name="lookup">The lookup key.</param>
        /// <param name="value">The default value.</param>
        /// <returns>The original bindable (not a bound copy).</returns>
        protected Bindable<TValue> SetDefault<TValue>(TLookup lookup, TValue value)
        {
            value = getDefault(lookup, value);

            Bindable<TValue> bindable = GetOriginalBindable<TValue>(lookup);

            if (bindable == null)
                bindable = set(lookup, value);
            else
                bindable.Value = value;

            bindable.Default = value;

            return bindable;
        }

        protected virtual void AddBindable<TBindable>(TLookup lookup, Bindable<TBindable> bindable)
        {
            ConfigStore[lookup] = bindable;
            bindable.ValueChanged += _ => QueueBackgroundSave();
        }

        private TValue getDefault<TValue>(TLookup lookup, TValue fallback)
        {
            if (defaultOverrides != null && defaultOverrides.TryGetValue(lookup, out object found))
                return (TValue)found;

            return fallback;
        }

        private Bindable<TValue> set<TValue>(TLookup lookup, TValue value)
        {
            Bindable<TValue> bindable = new Bindable<TValue>(value);
            AddBindable(lookup, bindable);
            return bindable;
        }

        public TValue Get<TValue>(TLookup lookup) => GetOriginalBindable<TValue>(lookup).Value;

        protected Bindable<TValue> GetOriginalBindable<TValue>(TLookup lookup)
        {
            if (ConfigStore.TryGetValue(lookup, out IBindable obj))
            {
                if (!(obj is Bindable<TValue>))
                    throw new InvalidCastException($"Cannot convert bindable of type {obj.GetType()} retrieved from {nameof(ConfigManager<TLookup>)} to {typeof(Bindable<TValue>)}.");

                return (Bindable<TValue>)obj;
            }

            return null;
        }

        /// <summary>
        /// Retrieve a bindable. This will be a new instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        public Bindable<TValue> GetBindable<TValue>(TLookup lookup) => GetOriginalBindable<TValue>(lookup)?.GetBoundCopy();

        /// <summary>
        /// Binds a local bindable with a configuration-backed bindable.
        /// </summary>
        public void BindWith<TValue>(TLookup lookup, Bindable<TValue> bindable) => bindable.BindTo(GetOriginalBindable<TValue>(lookup));

        public virtual TrackedSettings CreateTrackedSettings() => null;

        public void LoadInto(TrackedSettings settings) => settings.LoadFrom(this);

        public class TrackedSetting<TValue> : Tracking.TrackedSetting<TValue>
        {
            /// <summary>
            /// Constructs a new <see cref="TrackedSetting{TValue}"/>.
            /// </summary>
            /// <param name="setting">The config setting to be tracked.</param>
            /// <param name="generateDescription">A function that generates the description for the setting, invoked every time the value changes.</param>
            public TrackedSetting(TLookup setting, Func<TValue, SettingDescription> generateDescription)
                : base(setting, generateDescription)
            {
            }
        }
    }

    public abstract class ConfigManager : IDisposable
    {
        private bool hasLoaded;

        public void Load()
        {
            PerformLoad();
            hasLoaded = true;
        }

        private int lastSave;

        /// <summary>
        /// Queue a background save operation with debounce.
        /// </summary>
        protected void QueueBackgroundSave()
        {
            int current = Interlocked.Increment(ref lastSave);

            Task.Delay(100).ContinueWith(_ =>
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
