// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Audio.Asio
{
    // Minimal COM interop for Core Audio notifications (IMMNotificationClient)
    // Only implements the methods we need and forwards events to managed code.
    internal static class AsioDeviceNotifier
    {
        public static event Action? DeviceChanged;

        private static ImmNotificationClientImpl? client;
        private static IMmDeviceEnumerator? enumerator;

        public static void Start()
        {
            if (client != null) return;

            try
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                enumerator = (IMmDeviceEnumerator)new MMDeviceEnumerator();
                client = new ImmNotificationClientImpl();

                try
                {
                    // RegisterEndpointNotificationCallback returns an HRESULT; ignore non-zero gracefully
                    _ = enumerator.RegisterEndpointNotificationCallback(client);
                }
                catch
                {
                    // swallow registry failures and stop notifier to allow fallback polling
                    Stop();
                }
            }
            catch
            {
                // If CoreAudio APIs aren't available, silently fail - caller should fall back to polling.
                Stop();
            }
        }

        public static void Stop()
        {
            try
            {
                if (enumerator != null && client != null)
                {
                    _ = enumerator.UnregisterEndpointNotificationCallback(client);
                }
            }
            catch { }
            finally
            {
                client = null;
                enumerator = null;
            }
        }

        private class ImmNotificationClientImpl : IMmNotificationClient
        {
            public void OnDeviceStateChanged(string pwstrDeviceId, int dwNewState)
            {
                DeviceChanged?.Invoke();
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                DeviceChanged?.Invoke();
            }

            public void OnDeviceRemoved(string pwstrDeviceId)
            {
                DeviceChanged?.Invoke();
            }

            public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId)
            {
                DeviceChanged?.Invoke();
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                DeviceChanged?.Invoke();
            }
        }

        #region COM interop

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        private enum EDataFlow { ERender = 0, ECapture = 1, EAll = 2 }

        private enum ERole { EConsole = 0, EMultimedia = 1, ECommunications = 2 }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtID;
            public int pid;
        }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMmDeviceEnumerator
        {
            int NotImpl1();
            int NotImpl2();
            int RegisterEndpointNotificationCallback(IMmNotificationClient client);
            int UnregisterEndpointNotificationCallback(IMmNotificationClient client);
        }

        [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMmNotificationClient
        {
            void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, int dwNewState);
            void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
            void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
            void OnDefaultDeviceChanged(EDataFlow flow, ERole role, [MarshalAs(UnmanagedType.LPWStr)] string pwstrDefaultDeviceId);
            void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, PropertyKey key);
        }

        #endregion
    }
}
