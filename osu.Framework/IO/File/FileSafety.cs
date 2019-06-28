﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace osu.Framework.IO.File
{
    public static class FileSafety
    {
        public const int MAX_PATH_LENGTH = 248;

        public const string CLEANUP_DIRECTORY = @"_cleanup";

        public static string FilenameStrip(string entry)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                entry = entry.Replace(c.ToString(), string.Empty);
            return entry;
        }

        public static string PathStrip(string entry)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                entry = entry.Replace(c.ToString(), string.Empty);
            entry = entry.Replace(".", string.Empty);
            return entry;
        }

        // Adds an ACL entry on the specified directory for the specified account.
        // public static void AddDirectorySecurity(string filename, string account, FileSystemRights rights, InheritanceFlags inheritance, PropagationFlags propagation, AccessControlType controlType)
        // {
        //     // Create a new DirectoryInfo object.
        //     DirectoryInfo dInfo = new DirectoryInfo(filename);
        //     // Get a DirectorySecurity object that represents the
        //     // current security settings.
        //     DirectorySecurity dSecurity = dInfo.GetAccessControl();
        //     // Add the FileSystemAccessRule to the security settings.
        //     dSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, inheritance, propagation, controlType));
        //     // Set the new access settings.
        //     dInfo.SetAccessControl(dSecurity);
        // }

        public static void RemoveReadOnlyRecursive(string s)
        {
            var directory = new DirectoryInfo(s);

            foreach (var file in directory.GetFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    file.Attributes &= ~FileAttributes.ReadOnly;
            }

            foreach (var d in directory.GetDirectories())
                RemoveReadOnlyRecursive(d.FullName);
        }

        public static bool FileMove(string src, string dest, bool overwrite = true)
        {
            src = PathSanitise(src);
            dest = PathSanitise(dest);
            if (src == dest)
                return true; //no move necessary

            try
            {
                if (overwrite)
                    FileDelete(dest);
                System.IO.File.Move(src, dest);
            }
            catch
            {
                try
                {
                    System.IO.File.Copy(src, dest);
                    return FileDelete(src);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts all slashes and backslashes to OS-specific directory separator characters. Useful for sanitising user input.
        /// </summary>
        public static string PathSanitise(string path) => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);

        /// <summary>
        /// Converts all OS-specific directory separator characters to '/'. Useful for outputting to a config file or similar.
        /// </summary>
        public static string PathStandardise(string path) => path.Replace('\\', '/');

        [Flags]
        internal enum MoveFileFlags
        {
            None = 0,
            ReplaceExisting = 1,
            CopyAllowed = 2,
            DelayUntilReboot = 4,
            WriteThrough = 8,
            CreateHardlink = 16,
            FailIfNotTrackable = 32,
        }

        internal static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool MoveFileEx(
                string lpExistingFileName,
                string lpNewFileName,
                MoveFileFlags dwFlags);
        }

        public static bool FileDeleteOnReboot(string filename)
        {
            filename = PathSanitise(filename);

            try
            {
                System.IO.File.Delete(filename);
                return true;
            }
            catch
            {
            }

            string deathLocation = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                System.IO.File.Move(filename, deathLocation);
            }
            catch
            {
                deathLocation = filename;
            }

            return NativeMethods.MoveFileEx(deathLocation, null, MoveFileFlags.DelayUntilReboot);
        }

        /// <summary>
        /// Performs a file delete. If a delete operation fails (as can regularly happen on windows), the file is moved to a temporary directory for future cleanup (see <see cref="DeleteCleanupDirectory"/>).
        /// </summary>
        /// <param name="filename">The relative or full path of the file to delete.</param>
        /// <returns>True if deletion was successful by any means, or the file didn't exist.</returns>
        public static bool FileDelete(string filename)
        {
            var file = new FileInfo(PathSanitise(filename));

            if (!file.Exists) return true;

            try
            {
                file.Delete();
                return true;
            }
            catch
            {
            }

            try
            {
                //try alternative method: move to a cleanup folder and delete later.
                if (!Directory.Exists(CLEANUP_DIRECTORY))
                {
                    DirectoryInfo di = Directory.CreateDirectory(CLEANUP_DIRECTORY);
                    di.Attributes |= FileAttributes.Hidden;
                }

                file.MoveTo(Path.Combine(CLEANUP_DIRECTORY, Guid.NewGuid().ToString()));
                return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Delete any files in the cleanup directory (see <see cref="FileDelete(string)"/>).
        /// </summary>
        public static void DeleteCleanupDirectory()
        {
            if (Directory.Exists(CLEANUP_DIRECTORY))
                Directory.Delete(CLEANUP_DIRECTORY, true);
        }

        public static string AsciiOnly(string input)
        {
            if (input == null) return null;

            StringBuilder asc = new StringBuilder(input.Length);
            //keep only ascii chars
            foreach (char c in input)
                if (c <= 126)
                    asc.Append(c);
            return asc.ToString().Trim();
        }

        public static void RecursiveMove(string oldDirectory, string newDirectory)
        {
            oldDirectory = PathSanitise(oldDirectory);
            newDirectory = PathSanitise(newDirectory);

            if (oldDirectory == newDirectory)
                return;

            foreach (string dir in Directory.GetDirectories(oldDirectory))
            {
                string newSubDirectory = dir;
                newSubDirectory = Path.Combine(newDirectory, newSubDirectory.Remove(0, 1 + newSubDirectory.LastIndexOf(Path.DirectorySeparatorChar)));

                try
                {
                    DirectoryInfo newDirectoryInfo = Directory.CreateDirectory(newSubDirectory);

                    if (new DirectoryInfo(dir).Attributes.HasFlag(FileAttributes.Hidden))
                        newDirectoryInfo.Attributes |= FileAttributes.Hidden;
                }
                catch
                {
                }

                RecursiveMove(dir, newSubDirectory);
            }

            bool didExist = Directory.Exists(newDirectory);

            if (!didExist)
            {
                DirectoryInfo newDirectoryInfo = Directory.CreateDirectory(newDirectory);

                try
                {
                    if (new DirectoryInfo(oldDirectory).Attributes.HasFlag(FileAttributes.Hidden))
                        newDirectoryInfo.Attributes |= FileAttributes.Hidden;
                }
                catch
                {
                }
            }

            foreach (string file in Directory.GetFiles(oldDirectory))
            {
                string newFile = Path.Combine(newDirectory, Path.GetFileName(file));

                bool didMove = FileMove(file, newFile, didExist);

                if (!didMove)
                {
                    try
                    {
                        System.IO.File.Copy(file, newFile);
                    }
                    catch
                    {
                    }

                    System.IO.File.Delete(file);
                }
            }

            Directory.Delete(oldDirectory, true);
        }

        public static string GetExtension(string filename) => Path.GetExtension(filename)?.Trim('.').ToLower();

        public static int GetMaxPathLength(string directory)
        {
            int highestPathLength = directory.Length;

            foreach (string file in Directory.GetFiles(directory))
            {
                if (file.Length > highestPathLength)
                    highestPathLength = file.Length;
            }

            foreach (string dir in Directory.GetDirectories(directory))
            {
                int tempPathLength = GetMaxPathLength(dir);
                if (tempPathLength > highestPathLength)
                    highestPathLength = tempPathLength;
            }

            return highestPathLength;
        }

        public static string CleanStoryboardFilename(string filename) => PathStandardise(filename.Trim('"'));

        //This is better than encoding as it doesn't check for origin specific data or remove invalid chars.
        public static unsafe string RawBytesToString(byte[] encoded)
        {
            if (encoded.Length == 0)
                return string.Empty;

            char[] converted = new char[(encoded.Length + 1) / 2];

            fixed (byte* bytePtr = encoded)
            fixed (char* stringPtr = converted)
            {
                byte* stringBytes = (byte*)stringPtr;
                byte* stringEnd = (byte*)stringPtr + converted.Length * 2;
                byte* bytePtr2 = bytePtr;

                do
                {
                    *stringBytes = *bytePtr2++;
                    stringBytes++;
                } while (stringBytes != stringEnd);
            }

            return new string(converted);
        }

        // public static bool SetDirectoryPermissions(string directory)
        // {
        //     SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        //     NTAccount acct = sid.Translate(typeof(NTAccount)) as NTAccount;
        //     if (acct != null)
        //     {
        //         string strEveryoneAccount = acct.ToString();

        //         try
        //         {
        //             AddDirectorySecurity(directory, strEveryoneAccount, FileSystemRights.FullControl,
        //                 InheritanceFlags.None, PropagationFlags.NoPropagateInherit,
        //                 AccessControlType.Allow);
        //             AddDirectorySecurity(directory, strEveryoneAccount, FileSystemRights.FullControl,
        //                 InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
        //                 PropagationFlags.InheritOnly, AccessControlType.Allow);

        //             RemoveReadOnlyRecursive(directory);
        //         }
        //         catch
        //         {
        //             return false;
        //         }
        //     }

        //     return true;
        // }

        public static void CreateBackup(string filename)
        {
            string backupFilename = filename + @"." + DateTime.Now.Ticks + @".bak";

            if (System.IO.File.Exists(filename) && !System.IO.File.Exists(backupFilename))
            {
                Debug.Print(@"Backup created: " + backupFilename);
                System.IO.File.Move(filename, backupFilename);
            }
        }

        public static string GetRelativePath(string path, string folder)
        {
            path = PathStandardise(path).TrimEnd('/');
            folder = PathStandardise(folder).TrimEnd('/');

            if (path.Length < folder.Length + 1 || path[folder.Length] != '/' || !path.StartsWith(folder, StringComparison.Ordinal))
                throw new ArgumentException(path + " isn't contained in " + folder);

            return path.Substring(folder.Length + 1);
        }

        public static string GetTempPath(string suffix = "")
        {
            string directory = Path.Combine(Path.GetTempPath(), @"osu!");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, suffix);
        }

        /// <summary>
        /// Returns the path without the extension of the file.
        /// Contrarily to Path.GetFileNameWithoutExtension, it keeps the path to the file ("sb/triangle.png" becomes "sb/triangle" and not "triangle")
        /// </summary>
        public static string StripExtension(string filepath)
        {
            int dotIndex = filepath.LastIndexOf('.');
            return dotIndex == -1 ? filepath : filepath.Substring(0, dotIndex);
        }
    }
}
