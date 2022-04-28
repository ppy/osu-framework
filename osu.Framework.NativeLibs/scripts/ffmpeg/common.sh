#!/bin/bash

FFMPEG_VERSION=4.3.3

FFMPEG_FLAGS=(
    --disable-programs
    --disable-doc
    --disable-static
    --disable-debug
    --disable-ffplay
    --disable-ffprobe
    --disable-avdevice
    --disable-swresample
    --disable-librtmp
    --enable-shared
)

function build_ffmpeg() {
    echo "-> Downloading source..."

    curl https://ffmpeg.org/releases/ffmpeg-$FFMPEG_VERSION.tar.gz | tar zxf -
    cd ffmpeg-$FFMPEG_VERSION

    echo "-> Configuring..."

    ./configure "${FFMPEG_FLAGS[@]}"

    CORES=0
    if [[ "$OSTYPE" == "darwin"* ]]; then
        CORES=$(sysctl -n hw.ncpu)
    else
        CORES=$(nproc)
    fi

    echo "-> Building using $CORES threads..."

    make -j$CORES
    make install
}