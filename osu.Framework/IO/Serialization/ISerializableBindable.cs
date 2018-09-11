// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Newtonsoft.Json;
using osu.Framework.Configuration;

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
