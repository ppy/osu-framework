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
                    Foreground = DarkPlainText,
                    Background = DarkBackground,
                },
                new Style(ScopeName.HtmlServerSideScript)
                {
                    Background = Yellow,
                },
                new Style(ScopeName.HtmlComment)
                {
                    Foreground = DarkComment,
                },
                new Style(ScopeName.HtmlTagDelimiter)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.HtmlElementName)
                {
                    Foreground = DullRed,
                },
                new Style(ScopeName.HtmlAttributeName)
                {
                    Foreground = Red,
                },
                new Style(ScopeName.HtmlAttributeValue)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.HtmlOperator)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.Comment)
                {
                    Foreground = DarkComment,
                },
                new Style(ScopeName.XmlDocTag)
                {
                    Foreground = DarkXmlComment,
                },
                new Style(ScopeName.XmlDocComment)
                {
                    Foreground = DarkXmlComment,
                },
                new Style(ScopeName.String)
                {
                    Foreground = DarkString,
                },
                new Style(ScopeName.StringCSharpVerbatim)
                {
                    Foreground = DarkString,
                },
                new Style(ScopeName.Keyword)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.PreprocessorKeyword)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.HtmlEntity)
                {
                    Foreground = Red,
                },
                new Style(ScopeName.XmlAttribute)
                {
                    Foreground = DarkXmlAttribute,
                },
                new Style(ScopeName.XmlAttributeQuotes)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.XmlAttributeValue)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.XmlCDataSection)
                {
                    Foreground = DarkXamlCData,
                },
                new Style(ScopeName.XmlComment)
                {
                    Foreground = DarkComment,
                },
                new Style(ScopeName.XmlDelimiter)
                {
                    Foreground = DarkXmlDelimeter,
                },
                new Style(ScopeName.XmlName)
                {
                    Foreground = DarkXmlName,
                },
                new Style(ScopeName.ClassName)
                {
                    Foreground = DarkClass,
                },
                new Style(ScopeName.CssSelector)
                {
                    Foreground = DullRed,
                },
                new Style(ScopeName.CssPropertyName)
                {
                    Foreground = Red,
                },
                new Style(ScopeName.CssPropertyValue)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.SqlSystemFunction)
                {
                    Foreground = Magenta,
                },
                new Style(ScopeName.PowerShellAttribute)
                {
                    Foreground = PowderBlue,
                },
                new Style(ScopeName.PowerShellOperator)
                {
                    Foreground = DarkGray,
                },
                new Style(ScopeName.PowerShellType)
                {
                    Foreground = Teal,
                },
                new Style(ScopeName.PowerShellVariable)
                {
                    Foreground = OrangeRed,
                },

                new Style(ScopeName.Type)
                {
                    Foreground = Teal,
                },
                new Style(ScopeName.TypeVariable)
                {
                    Foreground = Teal,
                    Italic = true,
                },
                new Style(ScopeName.NameSpace)
                {
                    Foreground = Navy,
                },
                new Style(ScopeName.Constructor)
                {
                    Foreground = Purple,
                },
                new Style(ScopeName.Predefined)
                {
                    Foreground = Navy,
                },
                new Style(ScopeName.PseudoKeyword)
                {
                    Foreground = Navy,
                },
                new Style(ScopeName.StringEscape)
                {
                    Foreground = DarkGray,
                },
                new Style(ScopeName.ControlKeyword)
                {
                    Foreground = DarkKeyword,
                },
                new Style(ScopeName.Number)
                {
                    Foreground = DarkNumber
                },
                new Style(ScopeName.Operator),
                new Style(ScopeName.Delimiter),

                new Style(ScopeName.MarkdownHeader)
                {
                    Foreground = DarkKeyword,
                    Bold = true,
                },
                new Style(ScopeName.MarkdownCode)
                {
                    Foreground = DarkString,
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
                    Foreground = OliveDrab,
                    Bold = true,
                },
                new Style(ScopeName.BuiltinValue)
                {
                    Foreground = DarkOliveGreen,
                    Bold = true,
                },
                new Style(ScopeName.Attribute)
                {
                    Foreground = DarkCyan,
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
                Foreground = Color4.White;
            }

            /// <summary>
            /// Gets or sets the background color.
            /// </summary>
            /// <value>The background color.</value>
            public Color4? Background{ get; set; }

            /// <summary>
            /// Gets or sets the foreground color.
            /// </summary>
            /// <value>The foreground color.</value>
            public Color4 Foreground { get; set; }

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
