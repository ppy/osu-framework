// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using Newtonsoft.Json;
using osu.Framework.Lists;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// A converter used for serializing/deserializing <see cref="SortedList{T}"/> objects.
    /// </summary>
    internal class SortedListJsonConverter : JsonConverter<ISerializableSortedList>
    {
        public override void WriteJson(JsonWriter writer, ISerializableSortedList value, JsonSerializer serializer)
            => value.SerializeTo(writer, serializer);

        public override ISerializableSortedList ReadJson(JsonReader reader, Type objectType, ISerializableSortedList existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var iList = existingValue ?? (ISerializableSortedList)Activator.CreateInstance(objectType);
            Debug.Assert(iList != null);

            iList.DeserializeFrom(reader, serializer);

            return iList;
        }
    }
}
