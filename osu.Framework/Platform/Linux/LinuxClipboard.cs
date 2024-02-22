// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NuGet.Packaging;
using osu.Framework.Platform.SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace osu.Framework.Platform.Linux
{
    public class LinuxClipboard : SDL2Clipboard
    {
        protected Process Xclip(IEnumerable<string> args)
        {
            Process xclip = new Process();
            xclip.StartInfo.FileName = "xclip";
            xclip.StartInfo.UseShellExecute = false;
            xclip.StartInfo.RedirectStandardInput = true;
            xclip.StartInfo.RedirectStandardError = true;
            xclip.StartInfo.RedirectStandardOutput = true;
            xclip.StartInfo.ArgumentList.AddRange(args);
            return xclip;
        }

        protected bool HasTarget(string target)
        {
            using Process xclip = Xclip(new[] { "-selection", "clipboard", "-t", "TARGETS", "-o" });

            try
            {
                xclip.Start();

                bool hasTarget = false;

                string? line;
                while ((line = xclip.StandardOutput.ReadLine()) != null)
                {
                    if (line == target)
                    {
                        hasTarget = true;
                        break;
                    }
                }

                xclip.StandardInput.Close();
                xclip.WaitForExit();

                return hasTarget;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override Image<TPixel>? GetImage<TPixel>()
        {
            if (!HasTarget("image/png"))
                return base.GetImage<TPixel>();

            using Process xclip = Xclip(new[] { "-selection", "clipboard", "-t", "image/png", "-o" });

            try
            {
                xclip.Start();

                // reserve 5 MB because it's a bit above a typical png size
                using MemoryStream xclipStdout = new MemoryStream(5 * 1024 * 1024);

                // If we don't read stdout before waiting, the process might hang because some internal buffer got full.
                // I know, weird...
                xclip.StandardOutput.BaseStream.CopyTo(xclipStdout);
                xclipStdout.Position = 0;

                xclip.StandardInput.Close();
                xclip.WaitForExit();

                if (xclip.ExitCode == 0)
                    return Image.Load<TPixel>(xclipStdout);
                else
                    return null;
            }
            catch (Exception)
            {
                return base.GetImage<TPixel>();
            }
        }

        public override string? GetText()
        {
            if (!HasTarget("text/plain"))
                return base.GetText();

            using Process xclip = Xclip(new[] { "-selection", "clipboard", "-t", "text/plain", "-o" });

            try
            {
                xclip.Start();

                string? stdout = xclip.StandardOutput.ReadToEnd();

                xclip.StandardInput.Close();
                xclip.WaitForExit();

                if (xclip.ExitCode == 0)
                    return stdout;
                else
                    return null;
            }
            catch (Exception)
            {
                return base.GetText();
            }
        }

        public override bool SetImage(Image image)
        {
            using Process xclip = Xclip(new[] { "-selection", "clipboard", "-t", "image/png", "-i" });

            try
            {
                xclip.Start();

                image.Save(xclip.StandardInput.BaseStream, PngFormat.Instance);

                xclip.StandardInput.Close();
                xclip.WaitForExit();

                return xclip.ExitCode == 0;
            }
            catch (Exception)
            {
                return base.SetImage(image);
            }
        }

        public override void SetText(string text)
        {
            using Process xclip = Xclip(new[] { "-selection", "clipboard", "-t", "text/plain", "-i" });

            try
            {
                xclip.Start();

                xclip.StandardInput.Write(text);

                xclip.StandardInput.Close();
                xclip.WaitForExit();
            }
            catch (Exception)
            {
            }
        }
    }
}
