// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

namespace osu.Framework.Configuration.Tracking
{
    public interface ITrackableConfigManager
    {
        /// <summary>
        /// Retrieves all the settings of this <see cref="ConfigManager{T}"/> that are to be tracked for changes.
        /// </summary>
        /// <returns>A list of <see cref="ITrackedSetting"/>.</returns>
        TrackedSettings CreateTrackedSettings();

        /// <summary>
        /// Loads <see cref="Bindable{T}"/>s into <see cref="TrackedSettings"/>.
        /// </summary>
        /// <param name="settings">The settings to load into.</param>
        void LoadInto(TrackedSettings settings);
    }
}
