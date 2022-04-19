#!/bin/sh
# This script expects you to have llvm-mingw set up beforehand.
# You can get it here: https://github.com/mstorsjo/llvm-mingw

set -e

ffmpeg_version="4.3.2"

echo "Preparing FFMPEG source"
if [ ! -d "ffmpeg-${ffmpeg_version}" ]
then
    curl https://ffmpeg.org/releases/ffmpeg-"${ffmpeg_version}".tar.xz | tar xfJ -
fi
cd ffmpeg-"${ffmpeg_version}"

echo "Configuring FFMPEG build"
./configure --arch=aarch64 --target-os=mingw32 --cross-prefix=aarch64-w64-mingw32- \
--enable-shared --disable-static --prefix=build --disable-programs --disable-doc

echo "Compiling FFMPEG binaries"
make -j
make install

echo "Moving binaries to native folder"
cp build/bin/*.dll ../../native
