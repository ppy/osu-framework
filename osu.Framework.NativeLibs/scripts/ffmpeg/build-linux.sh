#!/bin/bash
set -eu

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

if [ -z "${arch-}" ]; then
    PS3='Build for which arch? '
    select arch in "x64" "arm64"; do
        if [ -z "$arch" ]; then
            echo "invalid option"
        else
            break
        fi
    done
fi

case $arch in
    x64)
        FFMPEG_FLAGS+=(
            --arch=x86_64
        )
        ;;

    arm64)
        FFMPEG_FLAGS+=(
            --arch=aarch64
        )
        ;;
esac

FFMPEG_FLAGS+=(
    --target-os=linux
)

pushd . > /dev/null
prep_ffmpeg "linux-$arch"
build_ffmpeg
popd > /dev/null

# gcc creates multiple symlinks per .so file for versioning.
# We delete the symlinks and rename the real files to include the major library version
rm linux-$arch/*.so
for f in linux-$arch/*.so.*.*.*; do
    mv -vf "$f" "${f%.*.*}"
done
