// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using YamlDotNet.Serialization;

namespace osu.Framework.MarkupLanguage
{
    /// <summary>
    /// An object holding the contents of an OML yaml file. This defines the properties and layout of <seealso cref="Drawable"/>s.
    /// </summary>
    public class OmlObject
    {
        internal static readonly string[] SPECIAL_PROPERTIES = { "Extends", "States", "Transitions", "Events", "Properties", "Children" };

        /// <summary>
        /// Contains general properties such as Width, Height and Colour. These are set when the <seealso cref="Drawable"/> is constructed.
        /// </summary>
        [YamlIgnore]
        public Dictionary<string, string> GeneralProperties = new Dictionary<string, string>();

        /// <summary>
        /// Objects are <seealso cref="Drawable"/>s by default and inherit all of <seealso cref="Drawable"/>'s properties. It is possible to change
        /// this by specifying the Extends property of the object.
        /// </summary>
        public Type Extends;

        /// <summary>
        /// A list of pre-defined properties to be used by <seealso cref="States"/>
        /// </summary>
        public Dictionary<string, OmlProperty> Properties;

        /// <summary>
        /// A state defines modifications to the value of the general properties of the object. This may be used to define named states which may be
        /// referred to in further code.
        ///
        /// The property named "Default" is optional but will be applied when the object is first constructed.
        /// </summary>
        public Dictionary<string, OmlState> States;

        /// <summary>
        /// A transition defines how an object should transition towards a state. This may be used to define named transitions which may be referred
        /// to in further code. These can transition to named or anonymous states.
        /// </summary>
        public Dictionary<string, OmlTransition> Transitions;

        /// <summary>
        /// An event defines the transitions applied to an object when the event is called. Events are exposed in the generated C# code. These can be
        /// used to apply named or anonymous transitions.
        /// </summary>
        public Dictionary<string, OmlEvent> Events;

        /// <summary>
        /// An object may be composited by several children that are directly affected by the containing object. In this way it is possible to
        /// transition an object and all of its children together by applying the transition to the containing object. (Read: transition applies to
        /// children)
        ///
        /// Children may be either named - allowing them to be referenced as private members from generated code, or anonymous.
        /// </summary>
        public OmlObject[] Children;

        /// <summary>
        /// A type with an optional default value
        /// </summary>
        public class OmlProperty
        {
            /// <summary>
            /// The type of this property
            /// </summary>
            public Type Type;    // TODO: is this needed?

            /// <summary>
            /// The default value of this property. Can be null.
            /// </summary>
            public string Value;
        }

        /// <summary>
        /// A collection of key-value pairs containing property names and their value.
        /// </summary>
        public class OmlState : Dictionary<string, object> { }

        /// <summary>
        /// A transition to an <seealso cref="OmlState"/>.
        /// </summary>
        public class OmlTransition
        {
            /// <summary>
            /// The end state of this transition
            /// </summary>
            public OmlState State;

            /// <summary>
            /// The duration of this transition. Defaults to 0 (instant)
            /// </summary>
            public float Duration = 0;

            /// <summary>
            /// The easing of this transition. Defaults to none (linear)
            /// </summary>
            public Easing Easing = Easing.None;
        }

        /// <summary>
        /// An event which may start some <seealso cref="OmlTransition"/>s.
        /// </summary>
        public class OmlEvent
        {
            /// <summary>
            /// A unique alias for this event
            /// </summary>
            public string AliasFor;

            /// <summary>
            /// Transitions to be ran when this event gets triggered.
            /// </summary>
            public OmlTransition[] Transitions;
        }
    }
}
