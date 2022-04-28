#!/bin/bash

curl https://ffmpeg.org/releases/ffmpeg-4.3.3.tar.gz | tar zxf -
cd ffmpeg-4.3.3

./configure \
    --disable-programs \
    --disable-doc \
    --disable-static \
    --disable-debug \
    --enable-shared \
    --target-os=linux \
    --prefix=build-x64

make -j$(nproc)
make install

mkdir -p ../build-x64
cp build-x64/lib/*.so ../build-x64/