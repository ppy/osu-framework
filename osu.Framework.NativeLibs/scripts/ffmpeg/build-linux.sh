#!/bin/bash
set -eu

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

FFMPEG_FLAGS+=(
    # --enable-vaapi
    # --enable-vdpau
    # --enable-hwaccel='h264_vaapi,h264_vdpau'
    # --enable-hwaccel='hevc_vaapi,hevc_vdpau'
    # --enable-hwaccel='vp8_vaapi,vp8_vdpau'
    # --enable-hwaccel='vp9_vaapi,vp9_vdpau'

    --target-os=linux
)

pushd . > /dev/null

if [ $(uname -m) == "x86_64" ]; then
    FFMPEG_FLAGS+=(
        --arch=x86_64
    )
    ARCH="x86_64"
elif [ $(uname -m) == "i686" ]; then
    FFMPEG_FLAGS+=(
        --arch=x86
    )
    ARCH="x86"

elif [ $(uname -m) == "aarch64" ]; then
    FFMPEG_FLAGS+=(
        --arch=arm64
    )
    ARCH="arm64"
elif [ $(uname -m) == "armv7l" ]; then
    FFMPEG_FLAGS+=(
        --arch=arm
    )
    ARCH="arm"
else
    echo "Unsupported architecture: $(uname -m)"
    exit 1
fi

prep_ffmpeg linux-$ARCH
# Apply patch from upstream to fix errors with new binutils versions:
# Ticket: https://fftrac-bg.ffmpeg.org/ticket/10405
# This patch should be removed when FFmpeg is updated to >=6.1
patch -p1 < "$SCRIPT_PATH/fix-binutils-2.41.patch"
build_ffmpeg
popd > /dev/null

# gcc creates multiple symlinks per .so file for versioning.
# We delete the symlinks and rename the real files to include the major library version
rm linux-x64/*.so
for f in linux-x64/*.so.*.*.*; do
    mv -vf "$f" "${f%.*.*}"
done
