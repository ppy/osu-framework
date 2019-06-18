// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.NetworkInformation;

namespace osu.Framework.IO.Network
{
    public static class NetworkHelper
    {
        public static bool NetworkAvailable => NetworkInterface.GetIsNetworkAvailable();
    }
}
