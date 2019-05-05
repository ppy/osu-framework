// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Framework.Graphics;
using YamlDotNet.Serialization;

namespace osu.Framework.MarkupLanguage
{
    public class OmlReader
    {
        // TODO: accept a dictionary of types for Extends (ctor or dep inj)
        public Drawable Load(string objectName, TextReader text)
        {
            var obj = Parse(objectName, text);

            // TODO
            return null;
        }

        public OmlObject Parse(string objectName, TextReader text) => ParseAll(text)[objectName];

        public Dictionary<string, OmlObject> ParseAll(TextReader text)
        {
            var deserializer = new Deserializer();
            var bla = deserializer.Deserialize<Dictionary<string, object>>(text);
            return parseChildren(bla);
        }

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

        private Type resolveType(object s)
        {
            return typeof(object);    // TODO
        }

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
    }
}
