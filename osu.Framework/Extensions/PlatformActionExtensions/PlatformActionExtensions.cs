// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input;

namespace osu.Framework.Extensions.PlatformActionExtensions
{
    public static class PlatformActionExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if this <paramref name="platformAction"/> is a common text editing <see cref="PlatformAction"/>.
        /// </summary>
        /// <remarks>
        /// Common text editing actions are those used for text editing in a text box,
        /// and/or are used by an platform-native IME for internal text editing purposes.
        /// </remarks>
        public static bool IsCommonTextEditingAction(this PlatformAction platformAction)
        {
            switch (platformAction)
            {
                case PlatformAction.Cut:
                case PlatformAction.Copy:
                case PlatformAction.Paste:
                case PlatformAction.SelectAll:
                case PlatformAction.MoveBackwardChar:
                case PlatformAction.MoveForwardChar:
                case PlatformAction.MoveBackwardWord:
                case PlatformAction.MoveForwardWord:
                case PlatformAction.MoveBackwardLine:
                case PlatformAction.MoveForwardLine:
                case PlatformAction.DeleteBackwardChar:
                case PlatformAction.DeleteForwardChar:
                case PlatformAction.DeleteBackwardWord:
                case PlatformAction.DeleteForwardWord:
                case PlatformAction.DeleteBackwardLine:
                case PlatformAction.DeleteForwardLine:
                case PlatformAction.SelectBackwardChar:
                case PlatformAction.SelectForwardChar:
                case PlatformAction.SelectBackwardWord:
                case PlatformAction.SelectForwardWord:
                case PlatformAction.SelectBackwardLine:
                case PlatformAction.SelectForwardLine:
                    return true;

                default:
                    return false;
            }
        }
    }
}
