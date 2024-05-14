#!/bin/bash
set -eu

FFMPEG_VERSION="7.0"
FFMPEG_FILE="ffmpeg-$FFMPEG_VERSION.tar.gz"
FFMPEG_FLAGS=(
    # General options
    --disable-static
    --enable-shared
    --disable-debug
    --disable-all
    --disable-autodetect
    --enable-lto

    # Libraries
    --enable-avcodec
    --enable-avformat
    --enable-swscale

    # Legacy video formats
    --enable-demuxer='avi,flv,asf'
    --enable-parser='mpeg4video'
    --enable-decoder='flv,msmpeg4v1,msmpeg4v2,msmpeg4v3,mpeg4,vp6,vp6f,wmv2'

    # Modern video formats
    --enable-demuxer='mov,matroska' # mov = mp4, matroska = mkv & webm
    --enable-parser='h264,hevc,vp8,vp9'
    --enable-decoder='h264,hevc,vp8,vp9'
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

    cd "$build_dir"
}

function build_ffmpeg() {
    echo "-> Configuring..."
    ./configure "${FFMPEG_FLAGS[@]}"

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
