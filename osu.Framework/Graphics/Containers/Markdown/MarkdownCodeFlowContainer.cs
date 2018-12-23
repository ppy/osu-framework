// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using ColorCode;
using ColorCode.Parsing;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownCodeFlowContainer : CustomizableTextContainer, IMarkdownTextComponent
    {
        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; }

        public MarkdownCodeFlowContainer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        public new void AddText(string text, Action<SpriteText> creationParameters = null)
            => base.AddText(text.Replace("[", "[[").Replace("]", "]]"), creationParameters);

        public void AddCodeText(string codeText,string languageCode)
        {
            if(string.IsNullOrEmpty(codeText))
                AddParagraph("");

            var formatter = new ClassFormatter(Styles);

            var language = Languages.FindById(languageCode);
            if (language == null)
                AddParagraph(codeText);
            else
            {
                //Change new line
                AddParagraph("");

                var codeSyntaxes = formatter.GetCodeSyntaxes(codeText, language);

                foreach (var codeSyntax in codeSyntaxes)
                    AddText(codeSyntax.ParsedSourceCode,
                        x =>
                        {
                            var style = codeSyntax.CodeStyle;
                            if(style == null)
                                return;

                            ApplyCodeText(x, style);
                        });
            }
        }

        SpriteText IMarkdownTextComponent.CreateSpriteText() => CreateSpriteText();

        protected virtual MarkdownCodeStyle Styles => new MarkdownCodeStyle().CreateDefaultStyle();

        protected virtual void ApplyCodeText(SpriteText text, MarkdownCodeStyle.Style codeStyle)
        {
            string font = "OpenSans-";
            if (codeStyle.Bold)
                font += "Bold";
            if (codeStyle.Italic)
                font += "Italic";

            text.Colour = codeStyle.Colour;
            text.ShadowColour = codeStyle.BackgroundColour ?? text.ShadowColour;
            text.Font = font.Trim('-');
        }

        public class ClassFormatter : CodeColorizerBase
        {
            private readonly List<CodeSyntax> codeSyntaxes = new List<CodeSyntax>();

            private readonly MarkdownCodeStyle markdownCodeStyle;

            public ClassFormatter(MarkdownCodeStyle style, ILanguageParser languageParser = null) : base(null, languageParser)
            {
                markdownCodeStyle = style;
            }

            public List<CodeSyntax> GetCodeSyntaxes(string sourceCode, ILanguage language)
            {
                codeSyntaxes.Clear();
                languageParser.Parse(sourceCode, language, (parsedSourceCode, captures) => Write(parsedSourceCode, captures));
                return codeSyntaxes;
            }

            protected override void Write(string parsedSourceCode, IList<Scope> scopes)
            {
                var scopeName = scopes.FirstOrDefault()?.Name;
                var style = new MarkdownCodeStyle.Style("unknown");

                if (!string.IsNullOrEmpty(scopeName) && markdownCodeStyle.Contains(scopeName))
                    style = markdownCodeStyle[scopeName];

                codeSyntaxes.Add(new CodeSyntax
                {
                    ParsedSourceCode = parsedSourceCode,
                    CodeStyle = style
                });
            }
        }

        public class CodeSyntax
        {
            /// <summary>
            /// Gets or sets the parsed source code
            /// </summary>
            /// <value>The background color.</value>
            public string ParsedSourceCode { get; set; }

            /// <summary>
            /// Gets or sets the parsed code style
            /// </summary>
            public MarkdownCodeStyle.Style CodeStyle { get; set; }
        }
    }
}
