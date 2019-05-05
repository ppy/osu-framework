// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osuTK;
using osuTK.Graphics;
using YamlDotNet.Serialization;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Framework.MarkupLanguage
{
    public class OmlReader
    {
        private readonly Dictionary<string, Type> allowedDrawables;

        public OmlReader(Dictionary<string, Type> allowedDrawables)
        {
            this.allowedDrawables = allowedDrawables;
        }

        public Drawable Load(string objectName, TextReader text)
        {
            var obj = Parse(objectName, text);
            return CreateDrawable(obj);
        }

        public Drawable CreateDrawable(OmlObject obj)
        {
            var type = obj.Extends ?? typeof(Container);
            var instance = (Drawable)Activator.CreateInstance(type);

            applyProperties(instance, obj.GeneralProperties);

            // TODO: set events

            if (obj.Children?.Any() == true)
                addChildren(instance, obj.Children);

            return instance;
        }

        public OmlObject Parse(string objectName, TextReader text) => ParseAll(text)[objectName];

        public Dictionary<string, OmlObject> ParseAll(TextReader text)
        {
            var deserializer = new Deserializer();
            var bla = deserializer.Deserialize<Dictionary<string, object>>(text);
            return parseChildren(bla);
        }

        #region YAML to object

        private Dictionary<string, OmlObject> parseChildren(Dictionary<string, object> bla)
        {
            var ret = new Dictionary<string, OmlObject>();

            foreach (var pair in bla)
                ret.Add(pair.Key, parseChild((Dictionary<object, object>)pair.Value));

            return ret;
        }

        private OmlObject[] parseChildren(List<object> bla)
        {
            var ret = new OmlObject[bla.Count];

            for (int i = 0; i < bla.Count; i++)
                ret[i] = parseChild((Dictionary<object, object>)bla[i]);

            return ret;
        }

        private OmlObject parseChild(Dictionary<object, object> values)
        {
            var obj = new OmlObject();

            // TODO: actually use these!
            if (values.ContainsKey("Properties"))
                obj.Properties = parseProperties(values["Properties"]);

            if (values.ContainsKey("Extends"))
                obj.Extends = resolveType(values["Extends"]);

            if (values.ContainsKey("States"))
                obj.States = parseStates(values["States"]);

            if (values.ContainsKey("Transitions"))
                obj.Transitions = parseTransitions(values["Transitions"], in obj);

            if (values.ContainsKey("Events"))
                obj.Events = parseEvents(values["Events"], in obj);

            if (values.ContainsKey("Children"))
                obj.Children = parseChildren((List<object>)values["Children"]);

            // add all others
            obj.GeneralProperties = new Dictionary<string, string>();
            foreach (KeyValuePair<object, object> pair in values.Where(x => !OmlObject.SPECIAL_PROPERTIES.Contains(x.Key)))
                obj.GeneralProperties.Add((string)pair.Key, (string)pair.Value);

            return obj;
        }

        private Type resolveType(object s) => allowedDrawables[(string)s];

        private Dictionary<string, OmlObject.OmlState> parseStates(object value)
        {
            var ret = new Dictionary<string, OmlObject.OmlState>();
            foreach (Dictionary<object, object> o in (List<object>)value) {
                var s = parseState(o, out string name);

                if (name == null)
                    throw new Exception("State had no name");

                ret[name] = s;
            }

            return ret;
        }

        private OmlObject.OmlState parseState(Dictionary<object, object> o, out string name)
        {
            var s = new OmlObject.OmlState();
            name = null;
            foreach ((string key1, string value1) in o.Select(x => ((string)x.Key, (string)x.Value))) {
                if (key1 != "Name")
                    s.Add(key1, value1);
                else
                    name = value1;
            }

            return s;
        }

        private Dictionary<string, OmlObject.OmlTransition> parseTransitions(object value, in OmlObject obj)
        {
            var ret = new Dictionary<string, OmlObject.OmlTransition>();

            foreach (Dictionary<object, object> o in (List<object>)value) {
                var t = parseTransition(o, in obj, out string name);

                if (name == null)
                    throw new Exception("Transition had no name");

                ret[name] = t;
            }

            return ret;
        }

        private OmlObject.OmlTransition parseTransition(Dictionary<object, object> o, in OmlObject obj, out string name)
        {
            var transition = new OmlObject.OmlTransition();
            name = null;

            foreach ((string key1, object value1) in o.Select(x => ((string)x.Key, x.Value))) {
                string valString = value1 as string;
                switch (key1) {
                    case "Name":
                        name = valString;
                        break;
                    case "Duration":
                        transition.Duration = float.Parse(valString, CultureInfo.InvariantCulture);
                        break;
                    case "Easing":
                        transition.Easing = (Easing)Enum.Parse(typeof(Easing), valString.Split('.').Last());
                        break;
                    case "State":
                        transition.State = valString != null
                            ? obj.States[valString]
                            : parseState((Dictionary<object, object>)value1, out _);
                        break;
                }
            }

            return transition;
        }

        private Dictionary<string, OmlObject.OmlEvent> parseEvents(object value, in OmlObject obj)
        {
            var ret = new Dictionary<string, OmlObject.OmlEvent>();

            foreach (Dictionary<object, object> o in (List<object>)value) {
                var t = parseEvent(o, in obj, out string name);

                if (name == null)
                    throw new Exception("Event had no name");

                ret[name] = t;
            }

            return ret;
        }

        private OmlObject.OmlEvent parseEvent(Dictionary<object, object> o, in OmlObject obj, out string name)
        {
            var e = new OmlObject.OmlEvent();
            name = null;

            foreach ((string key1, object value1) in o.Select(x => ((string)x.Key, x.Value))) {
                string valString = value1 as string;
                switch (key1) {
                    case "Name":
                        name = valString;
                        break;
                    case "Transitions":
                        var objs = (List<object>)value1;
                        e.Transitions = new OmlObject.OmlTransition[objs.Count];

                        for (int i = 0; i < objs.Count; i++) {
                            if (objs[i] is string s)
                                e.Transitions[i] = obj.Transitions[s];
                            else
                                e.Transitions[i] = parseTransition((Dictionary<object, object>)objs[i], in obj, out _);
                        }

                        break;
                }
            }

            return e;
        }

        private Dictionary<string, OmlObject.OmlProperty> parseProperties(object value)
        {
            var ret = new Dictionary<string, OmlObject.OmlProperty>();

            foreach (Dictionary<object, object> o in (List<object>)value) {
                var t = parseProperty(o, out string name);

                if (name == null)
                    throw new Exception("Property had no name");

                ret[name] = t;
            }

            return ret;
        }

        private OmlObject.OmlProperty parseProperty(Dictionary<object, object> o, out string name)
        {
            var p = new OmlObject.OmlProperty();
            name = null;

            foreach ((string key1, object value1) in o.Select(x => ((string)x.Key, x.Value))) {
                string valString = value1 as string;
                switch (key1) {
                    case "Name":
                        name = valString;
                        break;
                    case "Type":
                        p.Type = typeof(object);    // TODO
                        break;
                    case "Value":
                        p.Value = valString;
                        break;
                }
            }

            return p;
        }

        #endregion

        #region region populate object

        private void applyProperties(Drawable d, Dictionary<string, string> properties)
        {
            foreach (var pair in properties) {
                const BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var type = d.GetType();

                var prop = type.GetProperty(pair.Key, flags);
                if (prop != null) {
                    var value = safeConvertToObject(pair.Value, prop.PropertyType)
                                ?? throw new Exception($"Unable to convert value {pair.Value} to type {prop.PropertyType}");
                    prop.SetValue(d, value);
                    continue;
                }

                var field = type.GetField(pair.Key, flags);
                if (field != null) {
                    var value = safeConvertToObject(pair.Value, field.FieldType)
                                ?? throw new Exception($"Unable to convert value {pair.Value} to type {field.FieldType}");
                    field.SetValue(d, value);
                    continue;
                }

                throw new Exception($"Could not find property/field {pair.Key} on type {type}");
            }
        }

        private void addChildren(Drawable d, OmlObject[] objs)
        {
            if (!d.ExtendsClass(typeof(Container<>)))
                throw new InvalidOperationException($"Drawable '{d}' is not a container!");

            var children = objs.Select(CreateDrawable).ToArray();

            // Upcast the array so it fits the Children property of the container type (eg. RigidBodyContainer derive from Container<T>, and
            // Children is of type IReadOnlyList<T>. Passing it a Drawable[] would cause a runtime exception)
            Type t = findContainerGenericParam(d);
            var newChildren = Array.CreateInstance(t, children.Length);
            Array.Copy(children, newChildren, newChildren.Length);

            // Use reflection to set the Children property (we cannot cast to a generic type without parameter)
            d.GetType().GetProperty(nameof(Container.Children)).SetValue(d, newChildren);
        }

        private static object safeConvertToObject(string val, Type type)
        {
            var conv = TypeDescriptor.GetConverter(type);
            try {
                return conv.ConvertFromInvariantString(val);
            } catch (NotSupportedException) { }

            if (type == typeof(Vector2)) {
                string[] split = val.Split(',');
                if (split.Length != 2)
                    throw new Exception($"Passed {split.Length} numbers when 2 were expected for {nameof(Vector2)}");

                return new Vector2(float.Parse(split[0], CultureInfo.InvariantCulture), float.Parse(split[1], CultureInfo.InvariantCulture));
            }

            if (type == typeof(ColourInfo)) {
                // try color code
                if (val.StartsWith("#")) {
                    val = val.Substring(1);

                    switch (val.Length) {
                        case 6:
                            return (ColourInfo)new Color4(
                                Convert.ToByte(val.Substring(0, 2), 16),
                                Convert.ToByte(val.Substring(2, 2), 16),
                                Convert.ToByte(val.Substring(4, 2), 16),
                                0xFF);
                        case 3:
                            return (ColourInfo)new Color4(
                                (byte)(Convert.ToByte(val.Substring(0, 1), 16) * 0x11),
                                (byte)(Convert.ToByte(val.Substring(1, 1), 16) * 0x11),
                                (byte)(Convert.ToByte(val.Substring(2, 1), 16) * 0x11),
                                0xFF);
                        case 8:
                            return (ColourInfo)new Color4(
                                Convert.ToByte(val.Substring(0, 2), 16),
                                Convert.ToByte(val.Substring(2, 2), 16),
                                Convert.ToByte(val.Substring(4, 2), 16),
                                Convert.ToByte(val.Substring(6, 2), 16));
                        case 4:
                            return (ColourInfo)new Color4(
                                (byte)(Convert.ToByte(val.Substring(0, 1), 16) * 0x11),
                                (byte)(Convert.ToByte(val.Substring(1, 1), 16) * 0x11),
                                (byte)(Convert.ToByte(val.Substring(2, 1), 16) * 0x11),
                                (byte)(Convert.ToByte(val.Substring(3, 1), 16) * 0x11));
                    }
                }

                // try pre-defined code
                var c = typeof(Color4).GetProperty(val, BindingFlags.Static | BindingFlags.Public);
                if (c != null)
                    return (ColourInfo)(Color4)c.GetValue(null);

                // perhaps try osucolours in the future
                throw new Exception("Unrecognized color: " + val);
            }

            return null;
        }

        private static Type findContainerGenericParam(object o)
        {
            var type = o.GetType();
            var containerType = type.EnumerateBaseTypes().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Container<>));
            var genericType = containerType.GenericTypeArguments[0];
            return genericType;
        }

        #endregion
    }
}
