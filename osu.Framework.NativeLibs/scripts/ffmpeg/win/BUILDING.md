# Build Instructions

## Dependencies

```
sudo apt-get update
sudo apt-get install make nasm gcc mingw-w64
```

## `fetch_and_build.sh`
This script downloads ffmpeg 4.3.3 from ffmpeg.org and compiles it. You should run this three times - once for `x86`, `x64`, and `arm64`.

It outputs libraries into folders named `win-$arch` depending on the arch that was built.

## Cleanup
Files are left around for debugging purposes, manually delete the `win-$arch` and `ffmpeg-4.3.3` folders to cleanup.
