// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// A type of <see cref="JsonConverter"/> that serializes an <see cref="IReadOnlyList{T}"/> alongside
    /// each object's type in order to allow population of matching types via <see cref="JsonConvert.PopulateObject(string,object)"/>
    /// reconstruct the objects with their original types.
    /// </summary>
    /// <typeparam name="T">The type of objects contained in the <see cref="IReadOnlyList{T}"/> this attribute is attached to.</typeparam>
    internal class TypedRepopulatingConverter<T> : JsonConverter<IReadOnlyList<T>>
    {
        public override IReadOnlyList<T> ReadJson(JsonReader reader, Type objectType, IReadOnlyList<T> existingList, bool hasExistingValue, JsonSerializer serializer)
        {
            if (!hasExistingValue)
                throw new InvalidOperationException($"This converter is only meant to be used via {nameof(JsonConvert.PopulateObject)}");

            var obj = JToken.ReadFrom(reader);

            foreach (var tok in obj)
            {
                string typeName = tok["$type"]?.ToString();

                if (typeName == null)
                    throw new JsonException("Expected $type token.");

                var existing = existingList.FirstOrDefault(h => h.GetType() == Type.GetType(typeName));

                if (existing != null)
                    serializer.Populate(tok.CreateReader(), existing);
            }

            return existingList;
        }

        public override void WriteJson(JsonWriter writer, IReadOnlyList<T> value, JsonSerializer serializer)
        {
            var objects = new List<JObject>();

            foreach (var item in value)
            {
                var type = item.GetType();
                var assemblyName = type.Assembly.GetName();

                string typeString = $"{type.FullName}, {assemblyName.Name}";

                var itemObject = JObject.FromObject(item, serializer);
                itemObject.AddFirst(new JProperty("$type", typeString));
                objects.Add(itemObject);
            }

            serializer.Serialize(writer, objects);
        }
    }
}
