// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Newtonsoft.Json;
using osu.Framework.Lists;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// A converter used for serializing/deserializing <see cref="SortedList{T}"/> objects.
    /// </summary>
    public class SortedListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ISerializableSortedList).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var list = (ISerializableSortedList)value;
            list.SerializeTo(writer, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!(existingValue is ISerializableSortedList iList))
                iList = (ISerializableSortedList)Activator.CreateInstance(objectType);

            iList.DeserializeFrom(reader, serializer);

            return iList;
        }
    }
}
