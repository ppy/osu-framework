using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using osu.Framework.Bindables;
using osu.Framework.OML.Attributes;
using osu.Framework.OML.Objects;

namespace osu.Framework.OML.Factories
{
    public interface IOmlObjectFactory
    {
        OmlObject Create(string name, XElement element = null);
    }

    public class OmlObjectFactory : IOmlObjectFactory
    {
        private readonly OmlParser _parser;

        private readonly Dictionary<string, Type> _cachedObjectTypes =
            new Dictionary<string, Type>();

        public OmlObjectFactory(OmlParser parser)
        {
            _parser = parser;
        }

        public OmlObject Create(string name, XElement element = null)
        {
            OmlObject createdObject;

            if (_cachedObjectTypes.TryGetValue(name, out var cached))
            {
                createdObject = (OmlObject)Activator.CreateInstance(cached);
                applyAttributes(cached, createdObject, element);
                return createdObject;
            }

            var objectType = typeof(OmlObject);

            var objectAttributeType = typeof(OmlObjectAttribute);
            var types =
                AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(s => s.GetTypes())
                         .Where(p => objectType.IsAssignableFrom(p))
                         .Select(p => (Attribute.GetCustomAttribute(p, objectAttributeType), p))
                         .Where(p => ((OmlObjectAttribute)p.Item1)?.Aliases.Contains(name.ToLower()) == true)
                         .Select(p => p.p).ToImmutableArray();

            if (!types.Any())
                return new OmlObject(); // Create empty object if no Object with alias exists.

            var objType = types.FirstOrDefault();
            _cachedObjectTypes[name] = objType;

            createdObject = (OmlObject)Activator.CreateInstance(objType);

            if (element == null)
                return createdObject;

            applyAttributes(objType, createdObject, element);

            return createdObject;
        }

        private void applyAttributes(Type objType, OmlObject createdObject, XElement element)
        {
            createdObject.BindableValue = new Bindable<string>(element.Value);

            var objProps = objType.GetProperties()
                                  .ToArray();

            foreach (var objProp in objProps)
            {
                if (!objProp.CanWrite)
                    continue;

                var xamlAttribute = element
                                    .Attributes()
                                    .FirstOrDefault(a =>
                                        string.Equals(
                                            a.Name.LocalName,
                                            objProp.Name,
                                            StringComparison.CurrentCultureIgnoreCase
                                        )
                                    );

                if (xamlAttribute == null)
                    continue;

                var parsedAttribute = _parser.ParseAttribute(objProp.PropertyType, xamlAttribute);
                objProp.SetValue(createdObject, parsedAttribute);
            }
        }
    }
}
