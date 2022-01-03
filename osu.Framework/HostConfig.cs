// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework
{
    public struct HostConfig
    {
        public string GameName { get; set; }
        public bool BindIPC { get; set; }
        public bool PortableInstallation { get; set; }
        public bool BypassCompositor { get; set; }

        public static HostConfig GameConfig(string gameName, bool bindIPC = false, bool portableInstallation = false)
        {
            return new HostConfig
            {
                GameName = gameName,
                BindIPC = bindIPC,
                PortableInstallation = portableInstallation,
                BypassCompositor = true,
            };
        }

        public static HostConfig ApplicationConfig(string gameName, bool bindIPC = false, bool portableInstallation = false)
        {
            return new HostConfig
            {
                GameName = gameName,
                BindIPC = bindIPC,
                PortableInstallation = portableInstallation,
                BypassCompositor = false,
            };
        }
    }
}
