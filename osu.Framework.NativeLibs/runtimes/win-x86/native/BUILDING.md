# Building FFmpeg

The included FFmpeg libraries were cross-compiled on linux, if you wish to build them yourself:

```sh
# install dependencies (ubuntu)
sudo apt-get update
sudo apt-get install make nasm gcc mingw-w64

# grab & extract source
curl https://ffmpeg.org/releases/ffmpeg-4.3.3.tar.gz | tar zxf -
cd ffmpeg-4.3.3

# build 32-bit libs
./configure --disable-programs --disable-doc --disable-static --disable-debug --enable-shared --arch=x86 --target-os=mingw32 --cross-prefix=i686-w64-mingw32- --prefix=build-win32
make -j$(nproc)
make install

# build 64-bit libs
./configure --disable-programs --disable-doc --disable-static --disable-debug --enable-shared --arch=x86 --target-os=mingw32 --cross-prefix=x86_64-w64-mingw32- --prefix=build-win64
make -j$(nproc)
make install

# copy libs into place
cp build-win32/bin/*.dll /wherever/osu.Framework.NativeLibs/runtimes/win-x86/native/
cp build-win64/bin/*.dll /wherever/osu.Framework.NativeLibs/runtimes/win-x64/native/
```

Building on other platforms is left as an exercise for the reader.
