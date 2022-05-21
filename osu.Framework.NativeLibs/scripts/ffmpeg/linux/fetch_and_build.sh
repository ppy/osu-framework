#!/bin/bash

pushd $(dirname $0) > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source $SCRIPT_PATH/../common.sh

FFMPEG_FLAGS+=(
    --target-os=linux
    --prefix=build-x64
)

build_ffmpeg

mkdir -p ../build-x64
cp build-x64/lib/*.so ../build-x64/