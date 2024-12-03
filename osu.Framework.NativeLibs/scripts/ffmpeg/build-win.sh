#!/bin/bash
set -eu

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

if [ -z "${arch-}" ]; then
    PS3='Build for which arch? '
    select arch in "x86" "x64" "arm64"; do
        if [ -z "$arch" ]; then
            echo "invalid option"
        else
            break
        fi
    done
fi

cross_arch=''
cross_prefix=''

case $arch in
    x86)
        cross_arch='x86'
        cross_prefix='i686-w64-mingw32-'
        ;;

    x64)
        cross_arch='x86_64'
        cross_prefix='x86_64-w64-mingw32-'
        ;;

    arm64)
        cross_arch='aarch64'
        cross_prefix='aarch64-w64-mingw32-'
        ;;
esac

FFMPEG_FLAGS+=(
    --enable-w32threads

    --enable-dxva2
    --enable-d3d11va
    --enable-hwaccel='h264_dxva2,h264_d3d11va,h264_d3d11va2'
    --enable-hwaccel='hevc_dxva2,hevc_d3d11va,hevc_d3d11va2'
    --enable-hwaccel='vp9_dxva2,vp9_d3d11va,vp9_d3d11va2'

    --enable-cross-compile
    --target-os=mingw32
    --arch=$cross_arch
    --cross-prefix=$cross_prefix
)

pushd . > /dev/null
prep_ffmpeg "win-$arch"
build_ffmpeg
popd > /dev/null

find "win-$arch" -not -name "win-$arch" -not -name '*.dll' -delete
