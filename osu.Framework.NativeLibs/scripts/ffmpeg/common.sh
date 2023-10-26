#!/bin/bash
set -eu

FFMPEG_VERSION=4.3.3
FFMPEG_FILE="ffmpeg-$FFMPEG_VERSION.tar.gz"
FFMPEG_FLAGS=(
    # General options
    --disable-static
    --enable-shared
    --disable-all
    --disable-autodetect
    --enable-lto

    # Libraries
    --enable-avcodec
    --enable-avformat
    --enable-swscale

    # File and video formats
    --enable-demuxer='mov,matroska,flv,avi' # mov = mp4, matroska = mkv & webm
    --enable-parser='mpeg4video,h264,hevc,vp8,vp9'
    --enable-decoder='flv,mpeg4,h264,hevc,vp8,vp9'
    --enable-protocol=pipe
)

function prep_ffmpeg() {
    FFMPEG_FLAGS+=(
        --prefix="$PWD/$1"
        --shlibdir="$PWD/$1"
    )

    local build_dir="$1-build"
    if [ ! -e "$FFMPEG_FILE" ]; then
        echo "-> Downloading $FFMPEG_FILE..."
        curl -o "$FFMPEG_FILE" "https://ffmpeg.org/releases/$FFMPEG_FILE"
    else
        echo "-> $FFMPEG_FILE already exists, not re-downloading."
    fi

    if [ ! -d "$build_dir" ]; then
        echo "-> Unpacking source to $build_dir..."
        mkdir "$build_dir"
        tar xzf "$FFMPEG_FILE" --strip 1 -C "$build_dir"
    else
        echo "-> $build_dir already exists, skipping unpacking."
    fi

    echo "-> Configuring..."
    cd "$build_dir"
    ./configure "${FFMPEG_FLAGS[@]}"
}

function build_ffmpeg() {
    echo "-> Building using $CORES threads..."

    make -j$CORES
    make install-libs
}

CORES=0
if [[ "$OSTYPE" == "darwin"* ]]; then
    CORES=$(sysctl -n hw.ncpu)
else
    CORES=$(nproc)
fi
