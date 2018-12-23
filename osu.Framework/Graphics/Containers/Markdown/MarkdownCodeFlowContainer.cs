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
                var formatter = new LazerClassFormatter();
                var codeScopes = formatter.GetHtmlString(codeText, Languages.CSharp);

                foreach (var codeScope in codeScopes)
                {
                    AddText(codeScope.ParsedSourceCode,x => x.Colour = codeScope.Foreground);
                }

                AddParagraph("");
            }
        }

        SpriteText IMarkdownTextComponent.CreateSpriteText() => CreateSpriteText();

        public class LazerClassFormatter : CodeColorizerBase
        {
            /// <summary>
            /// Creates a <see cref="LazerClassFormatter"/>, for creating HTML to display Syntax Highlighted code, with Styles applied via CSS.
            /// </summary>
            /// <param name="Style">The Custom styles to Apply to the formatted Code.</param>
            /// <param name="languageParser">The language parser that the <see cref="HtmlClassFormatter"/> instance will use for its lifetime.</param>
            public LazerClassFormatter(StyleDictionary Style = null, ILanguageParser languageParser = null) : base(Style, languageParser)
            {

            }

            private List<CodeScope> CodeScopes = new List<CodeScope>();

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
                var color = Color4.White;
                if (!string.IsNullOrEmpty(scopeName) && Styles.Contains(scopeName))
                {
                    var colorText = Styles[scopeName].Foreground;
                    color = Color4.Aqua;
                }

                var codeScope = new CodeScope
                {
                    ParsedSourceCode = parsedSourceCode,
                    Foreground = color
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
            /// Gets or sets the background color.
            /// </summary>
            /// <value>The background color.</value>
            public Color4? BackgroundColour { get; set; }

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
