// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input
{
    public enum TextInputType
    {
        /// <summary>
        /// Plain text, default type of text input.
        /// </summary>
        Text,

        /// <summary>
        /// The text input is a person's name.
        /// </summary>
        Name,

        /// <summary>
        /// The text input is an email address.
        /// </summary>
        EmailAddress,

        /// <summary>
        /// The text input is a username.
        /// </summary>
        Username,

        /// <summary>
        /// The text input is numerical.
        /// </summary>
        Number,

        /// <summary>
        /// The text input is a password hidden from the user.
        /// </summary>
        Password,

        /// <summary>
        /// The text input is a numerical password hidden from the user.
        /// </summary>
        NumericalPassword,
    }

    public static class TextInputTypeExtensions
    {
        public static bool IsPassword(this TextInputType type)
        {
            switch (type)
            {
                case TextInputType.Password:
                case TextInputType.NumericalPassword:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsNumerical(this TextInputType type)
        {
            switch (type)
            {
                case TextInputType.Number:
                case TextInputType.NumericalPassword:
                    return true;

                default:
                    return false;
            }
        }

        public static bool SupportsIme(this TextInputType type) => type == TextInputType.Name || type == TextInputType.Text;
    }
}
