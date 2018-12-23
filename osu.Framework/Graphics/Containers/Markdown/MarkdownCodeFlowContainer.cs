using ColorCode;
using ColorCode.Parsing;
using ColorCode.Styling;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osuTK.Graphics;
using System.Collections.ObjectModel;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownCodeFlowContainer : CustomizableTextContainer, IMarkdownTextComponent
    {
        public float TotalTextWidth => Padding.TotalHorizontal + FlowingChildren.Sum(x => x.BoundingBox.Size.X);

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; }

        public MarkdownCodeFlowContainer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        public new void AddText(string text, Action<SpriteText> creationParameters = null)
            => base.AddText(text.Replace("[", "[[").Replace("]", "]]"), creationParameters);

        public void AddCodeText(string codeText,string language)
        {
            if(string.IsNullOrEmpty(codeText))
                AddParagraph("");

            if (string.IsNullOrEmpty(language))
            {
                AddText(codeText);
            }
            else
            {
                var formatter = new LazerClassFormatter(Styles);
                var codeScopes = formatter.GetHtmlString(codeText, Languages.CSharp);

                foreach (var codeScope in codeScopes)
                {
                    AddText(codeScope.ParsedSourceCode,x => x.Colour = codeScope.CodeStyle.Foreground);
                }

                AddParagraph("");
            }
        }

        SpriteText IMarkdownTextComponent.CreateSpriteText() => CreateSpriteText();

        protected virtual MarkdownCodeStyle Styles => new MarkdownCodeStyle().CreateDefaultStyle();

        public class LazerClassFormatter : CodeColorizerBase
        {
            /// <summary>
            /// Creates a <see cref="LazerClassFormatter"/>, for creating HTML to display Syntax Highlighted code, with Styles applied via CSS.
            /// </summary>
            /// <param name="style">The Custom styles to Apply to the formatted Code.</param>
            /// <param name="languageParser">The language parser that the <see cref="HtmlClassFormatter"/> instance will use for its lifetime.</param>
            public LazerClassFormatter(MarkdownCodeStyle style, ILanguageParser languageParser = null) : base(null, languageParser)
            {
                markdownCodeStyle = style;
            }

            private List<CodeScope> CodeScopes = new List<CodeScope>();

            private MarkdownCodeStyle markdownCodeStyle;

            /// <summary>
            /// Creates the HTML Markup, which can be saved to a .html file.
            /// </summary>
            /// <param name="sourceCode">The source code to colorize.</param>
            /// <param name="language">The language to use to colorize the source code.</param>
            /// <returns>Colorised HTML Markup.</returns>
            public List<CodeScope> GetHtmlString(string sourceCode, ILanguage language)
            {
                CodeScopes.Clear();

                languageParser.Parse(sourceCode, language, (parsedSourceCode, captures) => Write(parsedSourceCode, captures));

                return CodeScopes;
            }

            protected override void Write(string parsedSourceCode, IList<Scope> scopes)
            {
                var scopeName = scopes.FirstOrDefault()?.Name;
                var style = new MarkdownCodeStyle.Style("unknown");
                if (!string.IsNullOrEmpty(scopeName) && markdownCodeStyle.Contains(scopeName))
                {
                    style = markdownCodeStyle[scopeName];
                }

                var codeScope = new CodeScope
                {
                    ParsedSourceCode = parsedSourceCode,
                    CodeStyle = style
                };

                CodeScopes.Add(codeScope);
            }
        }

        public class CodeScope
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
