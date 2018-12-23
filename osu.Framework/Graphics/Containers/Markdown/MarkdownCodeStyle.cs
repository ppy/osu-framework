using osuTK.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ColorCode.Common;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownCodeStyle : KeyedCollection<string, MarkdownCodeStyle.Style>
    {
        protected override string GetKeyForItem(Style item)
        {
            return item.ScopeName;
        }

        protected virtual string Blue => "#FF0000FF";
        protected virtual string White => "#FFFFFFFF";
        protected virtual string Black => "#FF000000";
        protected virtual string DullRed => "#FFA31515";
        protected virtual string Yellow => "#FFFFFF00";
        protected virtual string Green => "#FF008000";
        protected virtual string PowderBlue => "#FFB0E0E6";
        protected virtual string Teal => "#FF008080";
        protected virtual string Gray => "#FF808080";
        protected virtual string Navy => "#FF000080";
        protected virtual string OrangeRed => "#FFFF4500";
        protected virtual string Purple => "#FF800080";
        protected virtual string Red => "#FFFF0000";
        protected virtual string MediumTurqoise => "FF48D1CC";
        protected virtual string Magenta => "FFFF00FF";
        protected virtual string OliveDrab => "#FF6B8E23";
        protected virtual string DarkOliveGreen => "#FF556B2F";
        protected virtual string DarkCyan => "#FF008B8B";

        protected virtual string VSDarkBackground => "#FF1E1E1E";
        protected virtual string VSDarkPlainText => "#FFDADADA";

        protected virtual string VSDarkXMLDelimeter => "#FF808080";
        protected virtual string VSDarkXMLName => "#FF#E6E6E6";
        protected virtual string VSDarkXMLAttribute => "#FF92CAF4";
        protected virtual string VSDarkXAMLCData => "#FFC0D088";
        protected virtual string VSDarkXMLComment => "#FF608B4E";

        protected virtual string VSDarkComment => "#FF57A64A";
        protected virtual string VSDarkKeyword => "#FF569CD6";
        protected virtual string VSDarkGray => "#FF9B9B9B";
        protected virtual string VSDarkNumber => "#FFB5CEA8";
        protected virtual string VSDarkClass => "#FF4EC9B0";
        protected virtual string VSDarkString => "#FFD69D85";

        public virtual MarkdownCodeStyle CreateDefaultStyle()
        {
            return new MarkdownCodeStyle
            {
                new Style(ScopeName.PlainText)
                {
                    Foreground = VSDarkPlainText,
                    Background = VSDarkBackground,
                },
                new Style(ScopeName.HtmlServerSideScript)
                {
                    Background = Yellow,
                },
                new Style(ScopeName.HtmlComment)
                {
                    Foreground = VSDarkComment,
                },
                new Style(ScopeName.HtmlTagDelimiter)
                {
                    Foreground = VSDarkKeyword,
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
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.HtmlOperator)
                {
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.Comment)
                {
                    Foreground = VSDarkComment,
                },
                new Style(ScopeName.XmlDocTag)
                {
                    Foreground = VSDarkXMLComment,
                },
                new Style(ScopeName.XmlDocComment)
                {
                    Foreground = VSDarkXMLComment,
                },
                new Style(ScopeName.String)
                {
                    Foreground = VSDarkString,
                },
                new Style(ScopeName.StringCSharpVerbatim)
                {
                    Foreground = VSDarkString,
                },
                new Style(ScopeName.Keyword)
                {
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.PreprocessorKeyword)
                {
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.HtmlEntity)
                {
                    Foreground = Red,
                },
                new Style(ScopeName.XmlAttribute)
                {
                    Foreground = VSDarkXMLAttribute,
                },
                new Style(ScopeName.XmlAttributeQuotes)
                {
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.XmlAttributeValue)
                {
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.XmlCDataSection)
                {
                    Foreground = VSDarkXAMLCData,
                },
                new Style(ScopeName.XmlComment)
                {
                    Foreground = VSDarkComment,
                },
                new Style(ScopeName.XmlDelimiter)
                {
                    Foreground = VSDarkXMLDelimeter,
                },
                new Style(ScopeName.XmlName)
                {
                    Foreground = VSDarkXMLName,
                },
                new Style(ScopeName.ClassName)
                {
                    Foreground = VSDarkClass,
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
                    Foreground = VSDarkKeyword,
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
                    Foreground = VSDarkGray,
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
                    Foreground = VSDarkGray,
                },
                new Style(ScopeName.ControlKeyword)
                {
                    Foreground = VSDarkKeyword,
                },
                new Style(ScopeName.Number)
                {
                    Foreground = VSDarkNumber
                },
                new Style(ScopeName.Operator)
                {

                },
                new Style(ScopeName.Delimiter)
                {

                },

                new Style(ScopeName.MarkdownHeader)
                {
                    Foreground = VSDarkKeyword,
                    Bold = true,
                },
                new Style(ScopeName.MarkdownCode)
                {
                    Foreground = VSDarkString,
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
                new Style(ScopeName.SpecialCharacter)
                {

                },
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
