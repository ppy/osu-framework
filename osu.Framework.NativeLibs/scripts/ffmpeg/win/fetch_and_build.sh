#!/bin/bash

cross_arch=''
cross_prefix=''

case $arch in
    x86)
        cross_arch='x86'
        cross_prefix='i686-w64-mingw32-'
        ;;

    x64)
        cross_arch='x86'
        cross_prefix='x86_64-w64-mingw32-'
        ;;

    arm64)
        cross_arch='aarch64'
        cross_prefix='aarch64-w64-mingw32-'
        ;;
esac

curl https://ffmpeg.org/releases/ffmpeg-4.3.3.tar.gz | tar zxf -
cd ffmpeg-4.3.3

./configure \
    --disable-programs \
    --disable-doc \
    --disable-static \
    --disable-debug \
    --enable-shared \
    --arch=$cross_arch \
    --target-os=mingw32 \
    --cross-prefix=$cross_prefix \
    --prefix=build-$arch

make -j$(nproc)
make install

mkdir -p ../build-$arch
cp build-$arch/bin/*.dll ../build-$arch/