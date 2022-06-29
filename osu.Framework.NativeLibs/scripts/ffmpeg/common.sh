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
    --disable-alsa
    --disable-iconv
    --disable-libxcb
    --disable-libxcb-shm
    --disable-libxcb-xfixes
    --disable-libxcb-shape
    --disable-sdl2
    --disable-zlib
    --disable-bzlib
    --disable-lzma
    --disable-xlib
    --disable-schannel
    --enable-shared
)

function build_ffmpeg() {
    if [ ! -d "ffmpeg-$FFMPEG_VERSION" ]; then
        echo "-> Downloading source..."
        curl https://ffmpeg.org/releases/ffmpeg-$FFMPEG_VERSION.tar.gz | tar zxf -
    else
        echo "-> ffmpeg-$FFMPEG_VERSION already exists, not re-downloading."
    fi

    echo "-> Configuring..."

    cd ffmpeg-$FFMPEG_VERSION
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