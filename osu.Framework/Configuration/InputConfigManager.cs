// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Handles serialisation/deserialisation of a provided collection of <see cref="InputHandler"/>s.
    /// </summary>
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

            bindToHandlersBindables();
        }

        protected override bool PerformSave()
        {
            try
            {
                using (var stream = storage.CreateFileSafely(FILENAME))
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

        /// <summary>
        /// Binds to all <see cref="Bindable{T}"/>s that the <see cref="InputHandlers"/> expose,
        /// and calls <see cref="ConfigManager.QueueBackgroundSave"/> when their values change.
        /// </summary>
        private void bindToHandlersBindables()
        {
            foreach (var handler in InputHandlers)
            {
                foreach (var property in handler.GetType().GetProperties())
                {
                    // get the underlying Bindable<T> for this property.
                    var bindableType = property.PropertyType.EnumerateBaseTypes().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Bindable<>));

                    // ignore if this isn't a bindable.
                    if (bindableType == null) continue;

                    // get the type that this Bindable<T> encapsulates.
                    var encapsulatedType = bindableType.GetGenericArguments()[0];

                    var subscribeMethod = typeof(InputConfigManager).GetMethod(nameof(subscribe), BindingFlags.NonPublic | BindingFlags.Instance);
                    Debug.Assert(subscribeMethod != null);

                    // call `subscribe` with the type and bindable.
                    subscribeMethod.MakeGenericMethod(encapsulatedType)
                                   .Invoke(this, new[] { property.GetValue(handler) });
                }
            }
        }

        private void subscribe<T>(Bindable<T> bindable) => bindable.BindValueChanged(_ => QueueBackgroundSave());
    }
}
