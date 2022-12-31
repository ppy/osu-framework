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
    public class Vector2DConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.ValueType == typeof(double))
            {
                double value = reader.ReadAsDouble() ?? double.NaN;
                
                if (objectType == typeof(Vector2d))
                    return new Vector2d(value, value);
                
                return value;
            }

            var obj = JObject.Load(reader);
            return new Vector2d((double)obj["x"], (double)obj["y"]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is double)
            {
                writer.WriteValue((double)value);
            }
            else
            {
                writer.WriteStartObject();

                var vector2DValue = (Vector2d)value;
                writer.WritePropertyName("x");
                writer.WriteValue(vector2DValue.X);
                writer.WritePropertyName("y");
                writer.WriteValue(vector2DValue.Y);

                writer.WriteEndObject();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2d) || objectType == typeof(double);
        }
    }
}
