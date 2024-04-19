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
prep_ffmpeg linux-x64
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
