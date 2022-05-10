# Build Instructions

## Dependencies

```
sudo apt-get update
sudo apt-get install make nasm gcc
```

## `fetch_and_build.sh`
This script downloads ffmpeg 4.3.3 from ffmpeg.org and compiles it.

It outputs libraries into a folder named `linux-x64`.

## Cleanup
Files are left around for debugging purposes, manually delete the `linux-x64` and `ffmpeg-4.3.3` folders to cleanup.
