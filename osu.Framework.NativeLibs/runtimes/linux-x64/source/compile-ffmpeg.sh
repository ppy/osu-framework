#!/bin/sh
#You may need to compile on a distro that has an older version of GLIBC to avoid some issues.
#Also, GCC 5.4.0 or Clang 3.8.0 is recommended.
#Tested on Ubuntu 16.04 and Ubuntu 20.10, works fine.

ffmpeg_version="4.3.2"

#Fetch tarball from ffmpeg.org and extract
curl https://ffmpeg.org/releases/ffmpeg-"${ffmpeg_version}".tar.xz | tar xfJ -

#Switch to `ffmpeg-4.3.1` directory and make `build` directory
cd ffmpeg-"${ffmpeg_version}" || exit 1;
mkdir build

#Configure
./configure --disable-ffplay --disable-ffprobe --disable-avdevice --disable-swresample \
--disable-static --enable-shared --prefix=build --libdir=../../native

#Build
make -j
make install

#Remove unused pkgconfig
cd ../../native
rm -r pkgconfig