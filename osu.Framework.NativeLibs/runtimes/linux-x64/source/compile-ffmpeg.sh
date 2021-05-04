#!/bin/sh
#Compile on a distro that has an older version of GLIBC/gcc is recommended to avoid some issues.
#Tested on Ubuntu21.04, works fine.
#Built on Deepin 20.2 with gcc-7 (Debian 7.4.0-6) 7.4.0, libc6 version 2.28.12-1+eagle
#Not using 4.4 because it causes "symbol lookup error: /tmp/.mount_osu!-xzhQaym/usr/bin/libavformat.so: undefined symbol: avpriv_packet_list_put, version LIBAVCODEC_58"

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