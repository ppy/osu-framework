// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Logging;

namespace osu.Framework.Platform.Windows.Native.NVAPI
{
    /// <summary>
    /// A NVAPI wrapper for changing NVIDIA driver behavior.
    /// </summary>
    public partial class NvLibrary
    {
        public static bool Initialized { get; private set; }

        /// <summary>
        /// Attempts to initialize NVAPI and gracefully fails if it can't
        /// </summary>
        public static void TryInitialize()
        {
            try
            {
                // Initialize all NVAPI function pointers
                initInterface = loadApiFunction<InitInterfaceDelegate>();
                drsCreateSession = loadApiFunction<DrsCreateSessionDelegate>();
                drsDestroySession = loadApiFunction<DrsDestroySessionDelegate>();
                drsLoadSettings = loadApiFunction<DrsLoadSettingsDelegate>();
                drsFindApplicationByName = loadApiFunction<DrsFindApplicationByNameDelegate>();
                drsCreateProfile = loadApiFunction<DrsCreateProfileDelegate>();
                drsCreateApplication = loadApiFunction<DrsCreateApplicationDelegate>();
                drsSetSetting = loadApiFunction<DrsSetSettingDelegate>();
                drsSaveSettings = loadApiFunction<DrsSaveSettingsDelegate>();

                // Initialize NVAPI
                CheckResult(initInterface());

                Initialized = true;
                Logger.Log("Successfully loaded NVAPI");
            }
            catch (DllNotFoundException)
            {
                // It can be reasonably assumed that NVAPI failed to load because the user isn't on a NVIDIA GPU
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to load NVAPI");
            }
        }

        private static T loadApiFunction<T>() where T : Delegate
        {
            var attr = (NvInterfaceAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(NvInterfaceAttribute));
            if (attr == null)
                throw new InvalidOperationException($"{nameof(T)} doesn't have a NvapiInterface attribute");

            IntPtr ptr = IntPtr.Size == 4 ? nvapi_QueryInterface32(attr.Id) : nvapi_QueryInterface64(attr.Id);
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        public static void CheckResult(NvStatus res)
        {
            if (res != NvStatus.OK)
                throw new InvalidOperationException($"NVAPI call failed: {res}");
        }

        [DllImport("nvapi.dll", EntryPoint = "nvapi_QueryInterface")]
        private static extern IntPtr nvapi_QueryInterface32(uint id);

        [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface")]
        private static extern IntPtr nvapi_QueryInterface64(uint id);

        public class DriverSettingsSession : IDisposable
        {
            private readonly IntPtr handle;

            public DriverSettingsSession()
            {
                if (!Initialized)
                    throw new InvalidOperationException("Cannot create a DRS session without initializing NVAPI");

                CheckResult(drsCreateSession(out handle));
                CheckResult(drsLoadSettings(handle));
            }

            /// <summary>
            /// Loads the graphics driver settings profile for the current application.
            /// </summary>
            /// <returns>A handle to the driver settings profile.</returns>
            public IntPtr LoadProfile(string gameName)
            {
                // Check if the profile already exists
                NvDrsApplicationV1 dummyApp = default;
                dummyApp.Version = NvDrsApplicationV1.TargetVersion;

                string gameExe = AppDomain.CurrentDomain.FriendlyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? AppDomain.CurrentDomain.FriendlyName : RuntimeInfo.GetFrameworkAssemblyPath();
                NvStatus result = drsFindApplicationByName(handle, gameExe, out IntPtr profileHandle, ref dummyApp);

                if (result == NvStatus.EXECUTABLE_NOT_FOUND)
                {
                    // Application wasn't found, create a new profile
                    NvDrsProfileV1 profile = default;
                    profile.Version = NvDrsProfileV1.TargetVersion;
                    profile.ProfileName = gameName;
                    profile.IsPredefined = 0;
                    CheckResult(drsCreateProfile(handle, ref profile, out profileHandle));

                    // Create application info
                    // Other NvDrsApplicationV1 variable can't be reused
                    NvDrsApplicationV1 app = default;
                    app.Version = NvDrsApplicationV1.TargetVersion;
                    app.IsPredefined = 0;
                    app.AppName = gameExe;
                    app.UserFriendlyName = gameName;
                    CheckResult(drsCreateApplication(handle, profileHandle, ref app));
                }
                else
                {
                    // Either the function succeeded or a different error was returned
                    CheckResult(result);
                }

                return profileHandle;
            }

            public void SetU32Setting(IntPtr profileHandle, NvSettingId id, uint value)
            {
                NvDrsSettingV1 setting = default;
                setting.Version = NvDrsSettingV1.TargetVersion;
                setting.SettingId = id;
                setting.SettingType = NvSettingType.DWORD;
                setting.SettingLocation = NvSettingLocation.CURRENT;
                setting.CurrentValue.U32Value = value;
                setting.PredefinedValue.U32Value = value;

                CheckResult(drsSetSetting(handle, profileHandle, ref setting));
            }

            public void SaveSettings()
            {
                CheckResult(drsSaveSettings(handle));
            }

            public void Dispose()
            {
                CheckResult(drsDestroySession(handle));
            }
        }
    }
}
