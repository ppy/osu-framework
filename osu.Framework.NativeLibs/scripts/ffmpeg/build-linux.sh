#!/bin/bash
set -eu

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

FFMPEG_FLAGS+=(
    --target-os=linux
)

pushd . > /dev/null

if [ $(uname -m) == "x86_64" ]; then
    ARCH="x64"
elif [ $(uname -m) == "i686" ]; then
    ARCH="x86"
elif [ $(uname -m) == "aarch64" ]; then
    ARCH="arm64"
elif [ $(uname -m) == "armv7l" ]; then
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
rm linux-$ARCH/*.so
for f in linux-$ARCH/*.so.*.*.*; do
    mv -vf "$f" "${f%.*.*}"
done
