// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input
{
    public enum PlatformAction
    {
        Cut,
        Copy,
        Paste,
        Delete,
        SelectAll,
        Save,
        Undo,
        Redo,
        Exit,
        MoveToListStart,
        MoveToListEnd,
        DocumentNew,
        DocumentPrevious,
        DocumentNext,
        DocumentClose,
        TabNew,
        TabRestore,

        // Text edit specific actions
        MoveBackwardChar,
        MoveForwardChar,
        DeleteBackwardChar,
        DeleteForwardChar,
        SelectBackwardChar,
        SelectForwardChar,

        MoveBackwardWord,
        MoveForwardWord,
        DeleteBackwardWord,
        DeleteForwardWord,
        SelectBackwardWord,
        SelectForwardWord,

        MoveBackwardLine,
        MoveForwardLine,
        DeleteBackwardLine,
        DeleteForwardLine,
        SelectBackwardLine,
        SelectForwardLine,

        ZoomIn,
        ZoomOut,
        ZoomDefault,
    }
}
