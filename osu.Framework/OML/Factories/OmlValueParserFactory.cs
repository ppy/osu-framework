// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.OML.Factories
{
    public interface IOmlValueParserFactory
    {
        IOmlValueParser<T> Create<T>();

        IOmlEnumParser CreateEnum();

        object Create(Type type, Type parserType);
    }

    public class OmlValueParserFactory : IOmlValueParserFactory
    {
        private readonly Dictionary<Type, object> _cachedParsers =
            new Dictionary<Type, object>();

        public IOmlValueParser<T> Create<T>()
        {
            var parser = (IOmlValueParser<T>)Create(typeof(T), typeof(IOmlValueParser<T>));
            parser.ParserFactory = this;
            return parser;
        }

        public IOmlEnumParser CreateEnum()
        {
            return (IOmlEnumParser)Create(typeof(Enum), typeof(IOmlEnumParser));
        }

        public object Create(Type type, Type parserType)
        {
            if (_cachedParsers.TryGetValue(type, out var cached))
                return cached;

            var interfaceType = parserType;
            var types =
                AppDomain.CurrentDomain
                         .GetAssemblies()
                         .SelectMany(s => s.GetTypes())
                         .Where(p => interfaceType.IsAssignableFrom(p)).ToList();

            var parser = types.LastOrDefault();
            if (parser == null)
                return null;

            var instance = Activator.CreateInstance(parser);
            parser.GetProperty("ParserFactory")?.SetValue(instance, this);
            _cachedParsers[type] = instance;

            return instance;
        }
    }
}
