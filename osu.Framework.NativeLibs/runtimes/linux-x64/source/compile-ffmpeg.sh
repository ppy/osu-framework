#!/bin/sh
#You may need to compile on a distro that has an older version of GLIBC to avoid some issues.
#Tested on Ubuntu 16.04 and Ubuntu 20.10, works fine.

#Extract tarball
xz -d ffmpeg-4.3.1.tar.xz
tar -xvf ffmpeg-4.3.1.tar

#Switch to `ffmpeg-4.3.1` directory and make `build` directory
cd ffmpeg-4.3.1 || exit 1;
mkdir build

#Configure
./configure --disable-ffplay --disable-ffprobe --disable-avdevice --disable-swresample \
--disable-static --enable-shared --prefix=build --libdir=build/lib

#Compile
make -j
make install