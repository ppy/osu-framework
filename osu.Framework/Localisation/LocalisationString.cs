using osu.Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Localisation
{
    public class LocalisableString
    {
        public Bindable<string> Text { get; }
        public Bindable<string> NonUnicode { get; }
        public Bindable<LocalisationType> Type { get; }
        public Bindable<object[]> Args { get; }

        public LocalisableString(string text, LocalisationType type, string nonUnicode = null, params object[] args)
        {
            Text = new Bindable<string>(text);
            NonUnicode = new Bindable<string>(nonUnicode);
            Type = new Bindable<LocalisationType>(type);
            Args = new Bindable<object[]>(args);
        }

        public static implicit operator LocalisableString(string unlocalised) => new LocalisableString(unlocalised, LocalisationType.None);
    }
}
