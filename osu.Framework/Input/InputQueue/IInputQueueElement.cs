// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Input.InputQueue
{
    public interface IInputQueueElement
    {
        bool Accept(INonPositionalInputVisitor visitor, bool allowBlocking = true);
        bool Accept(IPositionalInputVisitor visitor, Vector2 screenSpacePos);
    }
}
