// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// Smooths cursor input to relevant nodes and corners that noticably affect the cursor path.
    /// If the input is a raw/HD input this will return true for every corner.
    /// Set SmoothRawInput to true to keep behaviour for HD inputs.
    /// </summary>
    public class InputSmoother
    {
        private Vector2? lastRelevantPosition;

        private Vector2? lastActualPosition;

        private bool isRawInput;

        public bool SmoothRawInput { get; set; }

        public bool AddPosition(Vector2 position)
        {
            if (!SmoothRawInput)
            {
                if (isRawInput)
                {
                    lastRelevantPosition = position;
                    lastActualPosition = position;
                    return true;
                }

                // HD if it has fractions
                if (position.X - (float) Math.Truncate(position.X) != 0)
                    isRawInput = true;
            }

            if (lastRelevantPosition == null || lastActualPosition == null)
            {
                lastRelevantPosition = position;
                lastActualPosition = position;
                return true;
            }

            Vector2 diff = position - lastRelevantPosition.Value;
            float distance = diff.Length;
            Vector2 direction = diff / distance;

            Vector2 realDiff = position - lastActualPosition.Value;
            lastActualPosition = position;

            // don't update when it moved less than 10 pixels from the last position in a straight fashion
            if (distance < 10 && Vector2.Dot(direction, realDiff.Normalized()) > 0.7)
                return false;

            lastRelevantPosition = position;

            return true;
        }
    }
}
