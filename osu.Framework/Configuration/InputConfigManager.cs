// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Platform;

#nullable enable

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Handles serialisation/deserialisation of a provided collection of <see cref="InputHandler"/>s.
    /// </summary>
    /// <remarks>
    /// Only saves settings on disposal currently.
    /// </remarks>
    [Serializable]
    internal class InputConfigManager : ConfigManager
    {
        public const string FILENAME = "input.json";

        private readonly Storage storage;

        [JsonConverter(typeof(TypedRepopulatingConverter<InputHandler>))]
        public IReadOnlyList<InputHandler> InputHandlers { get; set; }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="storage">The storage to store the configuration file to.</param>
        /// <param name="inputHandlers">The collection of available input handlers. Settings will be loaded into existing instances.</param>
        public InputConfigManager(Storage storage, IReadOnlyList<InputHandler> inputHandlers)
        {
            this.storage = storage;
            InputHandlers = inputHandlers;

            Load();
        }

        protected override bool PerformSave()
        {
            try
            {
                using (var stream = storage.GetStream(FILENAME, FileAccess.Write, FileMode.Create))
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write(JsonConvert.SerializeObject(this));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error occurred when saving input configuration");
            }

            return false;
        }

        protected override void PerformLoad()
        {
            if (storage.Exists(FILENAME))
            {
                try
                {
                    using (Stream stream = storage.GetStream(FILENAME, FileAccess.Read, FileMode.Open))
                    using (var sr = new StreamReader(stream))
                    {
                        JsonConvert.PopulateObject(sr.ReadToEnd(), this, new JsonSerializerSettings
                        {
                            ObjectCreationHandling = ObjectCreationHandling.Reuse,
                            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                        });
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error occurred when parsing input configuration");
                }
            }
        }
    }
}
