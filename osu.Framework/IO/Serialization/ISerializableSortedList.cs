// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Newtonsoft.Json;
using osu.Framework.Lists;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// An interface which allows <see cref="SortedList{T}"/> to be json serialized/deserialized.
    /// </summary>
    [JsonConverter(typeof(SortedListJsonConverter))]
    internal interface ISerializableSortedList
    {
        void SerializeTo(JsonWriter writer, JsonSerializer serializer);
        void DeserializeFrom(JsonReader reader, JsonSerializer serializer);
    }
}
