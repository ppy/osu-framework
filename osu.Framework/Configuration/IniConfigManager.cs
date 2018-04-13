// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class IniConfigManager<T> : ConfigManager<T>
        where T : struct
    {
        /// <summary>
        /// The backing file used to store the config. Null means no persistent storage.
        /// </summary>
        protected virtual string Filename => @"game.ini";

        private readonly Storage storage;

        public IniConfigManager(Storage storage)
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

                        string key = line.Substring(0, equalsIndex).Trim();
                        string val = line.Remove(0, equalsIndex + 1).Trim();

                        if (!Enum.TryParse(key, out T lookup))
                            continue;

                        if (ConfigStore.TryGetValue(lookup, out IBindable b))
                            try
                            {
                                b.Parse(val);
                            }
                            catch (Exception e)
                            {
                                Logger.Log($@"Unable to parse config key {lookup}: {e}", LoggingTarget.Runtime, LogLevel.Important);
                            }
                        else if (AddMissingEntries)
                            Set(lookup, val);
                    }
                }
            }
        }

        protected override bool PerformSave()
        {
            if (string.IsNullOrEmpty(Filename)) return false;

            try
            {
                using (var stream = storage.GetStream(Filename, FileAccess.Write, FileMode.Create))
                using (var w = new StreamWriter(stream))
                {
                    foreach (var p in ConfigStore)
                        w.WriteLine(@"{0} = {1}", p.Key, p.Value);
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
