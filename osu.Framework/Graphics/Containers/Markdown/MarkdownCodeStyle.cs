// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK.Graphics;
using System.Collections.ObjectModel;
using ColorCode.Common;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownCodeStyle : KeyedCollection<string, MarkdownCodeStyle.Style>
    {
        protected override string GetKeyForItem(Style item)
        {
            return item.ScopeName;
        }

        protected virtual Color4 Blue => new Color4(0,0,255,255); //"#0000FF"
        protected virtual Color4 White => new Color4(255,255,255,255); //"#FFFFFF"
        protected virtual Color4 Black => new Color4(0,0,0,255); //"#000000"
        protected virtual Color4 DullRed => new Color4(163, 21, 21, 255);//"#A31515"
        protected virtual Color4 Yellow => new Color4(255, 255, 0,255);//"#FFFF00"
        protected virtual Color4 Green => new Color4(0, 128, 0,255);//"#008000"
        protected virtual Color4 PowderBlue => new Color4(176, 224, 230,255);//"#B0E0E6"
        protected virtual Color4 Teal => new Color4(0, 128, 12,255);//"#008080"
        protected virtual Color4 Gray => new Color4(128, 128, 128,255);//"#808080"
        protected virtual Color4 Navy => new Color4(0, 0, 128,255);//"#000080"
        protected virtual Color4 OrangeRed => new Color4(255, 69, 0,255);//"#FF4500"
        protected virtual Color4 Purple => new Color4(128, 0, 128,255);//"#800080"
        protected virtual Color4 Red => new Color4(255, 0, 0,255);//"#FF0000"
        protected virtual Color4 MediumTurqoise => new Color4(72, 209, 204,255);//"48D1CC"
        protected virtual Color4 Magenta => new Color4(255, 0, 255,255);//"FF00FF"
        protected virtual Color4 OliveDrab =>new Color4(107, 142, 35,255);//"#6B8E23"
        protected virtual Color4 DarkOliveGreen => new Color4(85, 107, 47,255);//"#556B2F"
        protected virtual Color4 DarkCyan => new Color4(0, 139, 139,255);//"#008B8B"

        protected virtual Color4 DarkBackground => new Color4(30, 30, 30,255);//"#1E1E1E"
        protected virtual Color4 DarkPlainText => new Color4(218, 218, 218,255);//"#DADADA"

        protected virtual Color4 DarkXmlDelimeter => new Color4(128, 128, 128,255);//"#808080"
        protected virtual Color4 DarkXmlName => new Color4(230, 230, 230,255);//"#E6E6E6"
        protected virtual Color4 DarkXmlAttribute => new Color4(146, 202, 244,255);//"#92CAF4"
        protected virtual Color4 DarkXamlCData => new Color4(192, 208, 136,255);//"#C0D088"
        protected virtual Color4 DarkXmlComment => new Color4(96, 139, 78,255);//"#608B4E"

        protected virtual Color4 DarkComment => new Color4(87, 166, 74,255);//"#57A64A"
        protected virtual Color4 DarkKeyword =>new Color4(86, 156, 214,255);//"#569CD6"
        protected virtual Color4 DarkGray => new Color4(155, 155, 155,255);//"#9B9B9B"
        protected virtual Color4 DarkNumber => new Color4(181, 206, 168,255);//"#B5CEA8"
        protected virtual Color4 DarkClass =>new Color4(78, 201, 176,255);//"#4EC9B0"
        protected virtual Color4 DarkString =>new Color4(214, 157, 133,255);//"#D69D85"

        public virtual MarkdownCodeStyle CreateDefaultStyle()
        {
            return new MarkdownCodeStyle
            {
                new Style(ScopeName.PlainText)
                {
                    Colour = DarkPlainText,
                    BackgroundColour = DarkBackground,
                },
                new Style(ScopeName.HtmlServerSideScript)
                {
                    BackgroundColour = Yellow,
                },
                new Style(ScopeName.HtmlComment)
                {
                    Colour = DarkComment,
                },
                new Style(ScopeName.HtmlTagDelimiter)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.HtmlElementName)
                {
                    Colour = DullRed,
                },
                new Style(ScopeName.HtmlAttributeName)
                {
                    Colour = Red,
                },
                new Style(ScopeName.HtmlAttributeValue)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.HtmlOperator)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.Comment)
                {
                    Colour = DarkComment,
                },
                new Style(ScopeName.XmlDocTag)
                {
                    Colour = DarkXmlComment,
                },
                new Style(ScopeName.XmlDocComment)
                {
                    Colour = DarkXmlComment,
                },
                new Style(ScopeName.String)
                {
                    Colour = DarkString,
                },
                new Style(ScopeName.StringCSharpVerbatim)
                {
                    Colour = DarkString,
                },
                new Style(ScopeName.Keyword)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.PreprocessorKeyword)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.HtmlEntity)
                {
                    Colour = Red,
                },
                new Style(ScopeName.XmlAttribute)
                {
                    Colour = DarkXmlAttribute,
                },
                new Style(ScopeName.XmlAttributeQuotes)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.XmlAttributeValue)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.XmlCDataSection)
                {
                    Colour = DarkXamlCData,
                },
                new Style(ScopeName.XmlComment)
                {
                    Colour = DarkComment,
                },
                new Style(ScopeName.XmlDelimiter)
                {
                    Colour = DarkXmlDelimeter,
                },
                new Style(ScopeName.XmlName)
                {
                    Colour = DarkXmlName,
                },
                new Style(ScopeName.ClassName)
                {
                    Colour = DarkClass,
                },
                new Style(ScopeName.CssSelector)
                {
                    Colour = DullRed,
                },
                new Style(ScopeName.CssPropertyName)
                {
                    Colour = Red,
                },
                new Style(ScopeName.CssPropertyValue)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.SqlSystemFunction)
                {
                    Colour = Magenta,
                },
                new Style(ScopeName.PowerShellAttribute)
                {
                    Colour = PowderBlue,
                },
                new Style(ScopeName.PowerShellOperator)
                {
                    Colour = DarkGray,
                },
                new Style(ScopeName.PowerShellType)
                {
                    Colour = Teal,
                },
                new Style(ScopeName.PowerShellVariable)
                {
                    Colour = OrangeRed,
                },

                new Style(ScopeName.Type)
                {
                    Colour = Teal,
                },
                new Style(ScopeName.TypeVariable)
                {
                    Colour = Teal,
                    Italic = true,
                },
                new Style(ScopeName.NameSpace)
                {
                    Colour = Navy,
                },
                new Style(ScopeName.Constructor)
                {
                    Colour = Purple,
                },
                new Style(ScopeName.Predefined)
                {
                    Colour = Navy,
                },
                new Style(ScopeName.PseudoKeyword)
                {
                    Colour = Navy,
                },
                new Style(ScopeName.StringEscape)
                {
                    Colour = DarkGray,
                },
                new Style(ScopeName.ControlKeyword)
                {
                    Colour = DarkKeyword,
                },
                new Style(ScopeName.Number)
                {
                    Colour = DarkNumber
                },
                new Style(ScopeName.Operator),
                new Style(ScopeName.Delimiter),

                new Style(ScopeName.MarkdownHeader)
                {
                    Colour = DarkKeyword,
                    Bold = true,
                },
                new Style(ScopeName.MarkdownCode)
                {
                    Colour = DarkString,
                },
                new Style(ScopeName.MarkdownListItem)
                {
                    Bold = true,
                },
                new Style(ScopeName.MarkdownEmph)
                {
                    Italic = true,
                },
                new Style(ScopeName.MarkdownBold)
                {
                    Bold = true,
                },

                new Style(ScopeName.BuiltinFunction)
                {
                    Colour = OliveDrab,
                    Bold = true,
                },
                new Style(ScopeName.BuiltinValue)
                {
                    Colour = DarkOliveGreen,
                    Bold = true,
                },
                new Style(ScopeName.Attribute)
                {
                    Colour = DarkCyan,
                    Italic = true,
                },
                new Style(ScopeName.SpecialCharacter),
            };
        }

        public class Style
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Style"/> class.
            /// </summary>
            /// <param name="scopeName">The name of the scope the style defines.</param>
            public Style(string scopeName)
            {
                ScopeName = scopeName;
                Colour = Color4.White;
            }

            /// <summary>
            /// Gets or sets the background color.
            /// </summary>
            /// <value>The background color.</value>
            public Color4? BackgroundColour{ get; set; }

            /// <summary>
            /// Gets or sets the foreground color.
            /// </summary>
            /// <value>The foreground color.</value>
            public Color4 Colour { get; set; }

            /// <summary>
            /// Gets or sets the name of the scope the style defines.
            /// </summary>
            /// <value>The name of the scope the style defines.</value>
            public string ScopeName { get; set; }

            /// <summary>
            /// Gets or sets italic font style.
            /// </summary>
            /// <value>True if italic.</value>
            public bool Italic { get; set; }

            /// <summary>
            /// Gets or sets bold font style.
            /// </summary>
            /// <value>True if bold.</value>
            public bool Bold { get; set; }
        }
    }
}
