#!/bin/bash

pushd $(dirname $0) > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source $SCRIPT_PATH/../common.sh

if [ -z "$arch" ]; then
    PS3='Build for which arch? '
    archs=("arm64" "x86_64")
    select arch in "${archs[@]}"; do
        case $arch in
            "arm64")
                break;;
            "x86_64")
                break;;
            *) echo "invalid option";;
        esac
    done
fi

FFMPEG_FLAGS+=(
    --target-os=darwin
    --arch=$arch
    --enable-cross-compile
    --extra-cflags='-arch $arch'
    --extra-ldflags='-arch $arch'
    --prefix=build-$arch
    --libdir=build-$arch/lib
)

build_ffmpeg

mv build-$arch/lib/libavcodec.58.91.100.dylib build-$arch/lib/libavcodec.58.dylib
mv build-$arch/lib/libavfilter.7.85.100.dylib build-$arch/lib/libavfilter.7.dylib
mv build-$arch/lib/libavformat.58.45.100.dylib build-$arch/lib/libavformat.58.dylib
mv build-$arch/lib/libavutil.56.51.100.dylib build-$arch/lib/libavutil.56.dylib
mv build-$arch/lib/libswscale.5.7.100.dylib build-$arch/lib/libswscale.5.dylib

echo "-> Fixing dylibs paths..."
BUILDPATH=build-$arch/lib
LIBS="libavcodec.58.dylib libavdevice.58.dylib libavfilter.7.dylib libavformat.58.dylib libavutil.56.dylib libswresample.3.dylib libswscale.5.dylib"
for f in $LIBS; do
    install_name_tool $BUILDPATH/$f -id @loader_path/$f \
        -change $BUILDPATH/libavcodec.58.dylib @loader_path/libavcodec.58.dylib \
        -change $BUILDPATH/libavfilter.7.dylib @loader_path/libavfilter.7.dylib \
        -change $BUILDPATH/libavformat.58.dylib @loader_path/libavformat.58.dylib \
        -change $BUILDPATH/libavutil.56.dylib @loader_path/libavutil.56.dylib \
        -change $BUILDPATH/libswscale.5.dylib @loader_path/libswscale.5.dylib

    mkdir -p ../build-$arch
    cp $BUILDPATH/$f ../build-$arch/$f
done
