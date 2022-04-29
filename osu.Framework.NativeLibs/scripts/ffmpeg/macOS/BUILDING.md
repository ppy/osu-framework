# Build Instructions

## Dependencies

See: https://trac.ffmpeg.org/wiki/CompilationGuide/macOS#CompilingFFmpegyourself

## `fetch_and_build.sh`
This script downloads ffmpeg 4.3.3 from ffmpeg.org and compiles it. You should run this twice - once for `x86_64` and for `arm64`.

It outputs dylibs into folders named `macOS-$arch` depending on the arch that was built.

## `combine_universal.sh`
Use this script to combine the `x86_64` and `arm64` dylibs built by the previous script into universal dylibs. The universal dylibs are output into a folder named `macOS-universal` and should be copied into `osu.Framework.NativeLibs/runtimes/osx`.

## Cleanup
Files are left around for debugging purposes, manually delete the `macOS-$arch` and `ffmpeg-4.3.3` folders to cleanup.
