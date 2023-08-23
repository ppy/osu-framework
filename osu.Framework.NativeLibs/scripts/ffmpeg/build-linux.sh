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
build_ffmpeg
popd > /dev/null

# gcc creates multiple symlinks per .so file for versioning.
# We want to delete the symlinks to prevent weird behaviour with GitHub actions.
rm linux-x64/*.so
for f in linux-x64/*.so.*.*.*; do
    mv -v "$f" "${f%.*.*.*}"
done
rm linux-x64/*.so.*
