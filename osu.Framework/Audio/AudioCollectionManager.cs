// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A collection of audio components which need central property control.
    /// </summary>
    public class AudioCollectionManager<T> : AdjustableAudioComponent
        where T : AdjustableAudioComponent
    {
        List<T> activeItems = new List<T>();

        protected void AddItem(T item)
        {
            item.AddAdjustmentDependency(this);
            activeItems.Add(item);
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < activeItems.Count; i++)
            {
                var item = activeItems[i];

                item.Update();

                if ((item as IHasCompletedState)?.HasCompleted ?? false)
                {
                    item.Dispose();
                    activeItems.RemoveAt(i--);
                }
            }
        }
    }
}
