// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Framework.Configuration;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// A converter used for serializing/deserializing <see cref="Bindable{T}"/> objects.
    /// </summary>
    internal class BindableJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ISerializableBindable).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bindable = (ISerializableBindable)value;
            bindable.SerializeTo(writer, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!(existingValue is ISerializableBindable bindable))
                bindable = (ISerializableBindable)Activator.CreateInstance(objectType, true);

            bindable.DeserializeFrom(reader, serializer);

            return bindable;
        }
    }
}
