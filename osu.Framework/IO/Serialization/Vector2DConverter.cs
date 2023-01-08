// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osuTK;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// A type of <see cref="JsonConverter"/> that serializes only the X and Y coordinates of a <see cref="Vector2d"/>.
    /// </summary>
    public class Vector2DConverter : JsonConverter<Vector2d>
    {
        public override Vector2d ReadJson(JsonReader reader, Type objectType, Vector2d existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.ValueType == typeof(double))
            {
                double value = (double)(reader.Value ?? double.NaN);
                return new Vector2d(value, value);
            }

            var obj = JObject.Load(reader);
            return new Vector2d((double)obj["x"], (double)obj["y"]);
        }

        public override void WriteJson(JsonWriter writer, Vector2d value, JsonSerializer serializer)
        {                
            writer.WriteStartObject();

            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);

            writer.WriteEndObject();          
        }
    }
}
