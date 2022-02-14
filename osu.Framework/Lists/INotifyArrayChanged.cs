﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Lists
{
    /// <summary>
    /// Provides an event for notifying about array elements changing.
    /// </summary>
    public interface INotifyArrayChanged
    {
        /// <summary>
        /// Invoked when an element of an array is changed.
        /// </summary>
        event Action ArrayElementChanged;
    }
}
