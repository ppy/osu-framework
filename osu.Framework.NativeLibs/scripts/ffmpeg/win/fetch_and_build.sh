#!/bin/bash

pushd $(dirname $0) > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source $SCRIPT_PATH/../common.sh

if [ -z "$arch" ]; then
    PS3='Build for which arch? '
    archs=("x86" "x64" "arm64")
    select arch in "${archs[@]}"; do
        case $arch in
            "x86")
                break;;
            "x64")
                break;;
            "arm64")
                break;;
            *) echo "invalid option";;
        esac
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
        cross_arch='x86'
        cross_prefix='x86_64-w64-mingw32-'
        ;;

    arm64)
        cross_arch='aarch64'
        cross_prefix='aarch64-w64-mingw32-'
        ;;
esac

FFMPEG_FLAGS+=(
    --arch=$cross_arch
    --target-os=mingw32
    --cross-prefix=$cross_prefix
    --prefix=build-$arch
)

build_ffmpeg

mkdir -p ../build-$arch
cp build-$arch/bin/*.dll ../build-$arch/