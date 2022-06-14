// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Newtonsoft.Json;
using osu.Framework.Bindables;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// An interface which allows <see cref="Bindable{T}"/> to be json serialized/deserialized.
    /// </summary>
    [JsonConverter(typeof(BindableJsonConverter))]
    internal interface ISerializableBindable
    {
        void SerializeTo(JsonWriter writer, JsonSerializer serializer);
        void DeserializeFrom(JsonReader reader, JsonSerializer serializer);
    }
}
