// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Xml.Linq;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.OML.Factories;
using osu.Framework.OML.Objects;

namespace osu.Framework.OML
{
    public interface IOmlParser
    {
        OmlObject ConstructContainer();

        T ParseAttribute<T>(XAttribute attribute, T def = default) where T : struct;
        object ParseAttribute(Type type, XAttribute attribute, object def = default);
    }

    public class OmlParser : IOmlParser
    {
        private readonly XDocument _xdoc;
        private readonly IOmlValueParserFactory _parserFactory;
        private readonly IOmlObjectFactory _objectFactory;

        public OmlParser(string data, IOmlValueParserFactory parserFactory = null, IOmlObjectFactory objectFactory = null)
        {
            _xdoc = XDocument.Parse(data);

            _parserFactory = parserFactory ?? new OmlValueParserFactory();
            _objectFactory = objectFactory ?? new OmlObjectFactory(this);
        }

        public OmlObject ConstructContainer()
        {
            if (_xdoc.Root == null || _xdoc.Root.Name.LocalName.ToLower() != "oml")
            {
                Logger.LogPrint("OML Root is non existent!", LoggingTarget.Runtime, LogLevel.Important);
                return null;
            }

            OmlObject obj = null;

            if (_xdoc.Root != null)
            {
                obj = constructContainers(_xdoc.Root);

                obj.RelativeSizeAxes = Axes.Both;
                obj.Anchor = Anchor.Centre;
                obj.Origin = Anchor.Centre;
                obj.FillMode = FillMode.Stretch;
            }

            return obj ?? _objectFactory.Create(_xdoc.Root.Name.LocalName, _xdoc.Root);
        }

        private OmlObject constructContainers(XElement element)
        {
            if (element == null)
                throw new NullReferenceException("No Root Container!");

            OmlObject obj = _objectFactory.Create(element.Name.LocalName, element);

            foreach (var e in element.Elements())
            {
                var childObject = constructContainers(e); // Construct Children

                obj.Add(childObject);
            }

            return obj;
        }

        public T ParseAttribute<T>(XAttribute attribute, T def = default) where T : struct
        {
            return (T)ParseAttribute(typeof(T), attribute, def);
        }

        public object ParseAttribute(Type type, XAttribute attribute, object def = default)
        {
            if (attribute == null)
                return def;

            if (type == typeof(string)) // This doesn't have to be in a value parser.
                return attribute.Value;

            if (type.IsEnum)
            {
                var parser = _parserFactory.CreateEnum();

                if (parser == null)
                    Logger.LogPrint($"Parser for type {type} was never implemented!", LoggingTarget.Runtime, LogLevel.Error);

                return parser?.Parse(type, attribute.Value) ?? def;
            }
            else
            {
                var parserType = typeof(IOmlValueParser<>).MakeGenericType(type);
                var parser = (IOmlValueParser)_parserFactory.Create(type, parserType);

                if (parser == null)
                    Logger.LogPrint($"Parser for type {type} was never implemented!", LoggingTarget.Runtime, LogLevel.Error);

                return parser?.Parse(type, attribute.Value) ?? def;
            }
        }
    }
}
