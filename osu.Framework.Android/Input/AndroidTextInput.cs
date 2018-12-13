// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Android.Input
{
    public class AndroidTextInput : ITextInputSource
    {
        private readonly AndroidGameView view;

        private string pending = string.Empty;

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
        }
        public bool ImeActive => false;

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;

        public void Activate(object sender)
        {

        }

        public void Deactivate(object sender)
        {
            
        }

        public string GetPendingText()
        {
            try
            {
                return pending;
            }
            finally
            {
                pending = string.Empty;
            }
        }
    }
}
