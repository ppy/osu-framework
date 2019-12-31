// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Stores;

namespace osu.Framework.Audio
{
    public interface IAdjustableResourceStore<T> : IResourceStore<T>, IAdjustableAudioComponent, IAggregateAudioAdjustment
        where T : AudioComponent
    {
    }
}
