# Build Instructions

## `fetch_and_build.sh`
This script downloads ffmpeg 4.3.3 from ffmpeg.org and compiles it. You should run this twice - once for x86_64 and then arm64 (on relevant hardware as required).
Copy the resulting `build-$arch` folders to the same machine for the following script.

## `combine_universal.sh`
Use this script to combine the x86_64 and arm64 dylibs built by the previous script into universal dylibs. The script then copies the universal dylibs into place (`osu.Framework.NativeLibs/runtimes/osx`) when done.

## Cleanup
Files are left around for debugging purposes, manually delete the `build-$arch` and `ffmpeg-4.3.3` folders to cleanup.