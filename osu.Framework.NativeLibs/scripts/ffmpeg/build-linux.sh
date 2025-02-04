#!/bin/bash
set -eu

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

if [ "$(dpkg --print-architecture)" = "amd64" ]; then
    arch="x64"
elif [ "$(dpkg --print-architecture)" = "i386" ]; then
    arch="x86"
elif [ "$(dpkg --print-architecture)" = "arm64" ]; then
    arch="arm64"
elif [ "$(dpkg --print-architecture)" = "armhf" ]; then
    arch="arm"
else
    echo "Unsupported architecture: $(dpkg --print-architecture)"
    exit 1
fi

FFMPEG_FLAGS+=(
    --target-os=linux
)

pushd . > /dev/null
prep_ffmpeg linux-$arch
build_ffmpeg
popd > /dev/null

# gcc creates multiple symlinks per .so file for versioning.
# We delete the symlinks and rename the real files to include the major library version
rm linux-$arch/*.so
for f in linux-$arch/*.so.*.*.*; do
    mv -vf "$f" "${f%.*.*}"
done
