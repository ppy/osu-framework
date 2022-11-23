// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class IniConfigManager<TLookup> : ConfigManager<TLookup>
        where TLookup : struct, Enum
    {
        /// <summary>
        /// The backing file used to store the config. Null means no persistent storage.
        /// </summary>
        protected virtual string Filename => @"game.ini";

        private readonly Storage storage;

        public IniConfigManager(Storage storage, IDictionary<TLookup, object> defaultOverrides = null)
            : base(defaultOverrides)
        {
            this.storage = storage;

            InitialiseDefaults();
            Load();
        }

        protected override void PerformLoad()
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

                        string key = line.AsSpan(0, equalsIndex).Trim().ToString();
                        string val = line.AsSpan(equalsIndex + 1).Trim().ToString();

                        if (!Enum.TryParse(key, out TLookup lookup))
                            continue;

                        if (ConfigStore.TryGetValue(lookup, out IBindable b))
                        {
                            try
                            {
                                if (!(b is IParseable parseable))
                                    throw new InvalidOperationException($"Bindable type {b.GetType().ReadableName()} is not {nameof(IParseable)}.");

                                parseable.Parse(val);
                            }
                            catch (Exception e)
                            {
                                Logger.Log($@"Unable to parse config key {lookup}: {e}", LoggingTarget.Runtime, LogLevel.Important);
                            }
                        }
                        else if (AddMissingEntries)
                            SetDefault(lookup, val);
                    }
                }
            }
        }

        protected override bool PerformSave()
        {
            if (string.IsNullOrEmpty(Filename)) return false;

            try
            {
                using (var stream = storage.CreateFileSafely(Filename))
                using (var w = new StreamWriter(stream))
                {
                    foreach (var p in ConfigStore)
                        w.WriteLine(@"{0} = {1}", p.Key, p.Value.ToString(CultureInfo.InvariantCulture).AsNonNull().Replace("\n", "").Replace("\r", ""));
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
