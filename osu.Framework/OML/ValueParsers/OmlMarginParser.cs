// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;

namespace osu.Framework.OML.ValueParsers
{
    public class OmlMarginParser : IOmlValueParser<MarginPadding>
    {
        public MarginPadding Parse(string value)
        {
            return (MarginPadding)Parse(typeof(MarginPadding), value);
        }

        public object Parse(Type type, string value)
        {
            var padding = new MarginPadding();

            var paddingValues = value.Split(',');

            // TODO: Implement auto keyword. as i have no clue how to implement it at this point of time.
            switch (paddingValues.Length)
            {
                case 1:
                    padding = new MarginPadding(float.Parse(paddingValues[0]));
                    break;

                case 2:
                    padding.Top = float.Parse(paddingValues[0]);
                    padding.Bottom = float.Parse(paddingValues[1]);
                    break;

                case 3:
                    padding.Top = float.Parse(paddingValues[0]);
                    padding.Bottom = float.Parse(paddingValues[1]);
                    padding.Left = float.Parse(paddingValues[2]);
                    break;

                case 4:
                    padding.Top = float.Parse(paddingValues[0]);
                    padding.Bottom = float.Parse(paddingValues[1]);
                    padding.Left = float.Parse(paddingValues[2]);
                    padding.Right = float.Parse(paddingValues[3]);
                    break;
            }

            return padding;
        }
    }
}
