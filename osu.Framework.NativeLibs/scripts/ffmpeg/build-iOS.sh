#!/bin/bash
set -eu

# Minimum iOS version. This should be the same as in osu.Framework.iOS.csproj
DEPLOYMENT_TARGET="13.4"

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

if [ -z "${GAS_PREPROCESSOR:-}" ]; then
    echo "GAS_PREPROCESSOR must be set"
    exit 1
fi

if [ -z "${arch-}" ]; then
    PS3='Build for which arch? '
    select arch in "arm64" "simulator-arm64" "simulator-x86_64"; do
        if [ -z "$arch" ]; then
            echo "invalid option"
        else
            break
        fi
    done
fi

cpu=''
cross_arch=''
cc=''
as=''
cflags=''

case $arch in
    arm64)
        cpu='armv8-a'
        cross_arch='arm64'
        cc='xcrun -sdk iphoneos clang'
        as="$GAS_PREPROCESSOR -arch arm64 -- $cc"
        cflags="-mios-version-min=$DEPLOYMENT_TARGET"
        ;;

    simulator-arm64)
        cpu='armv8-a'
        cross_arch='arm64'
        cc='xcrun -sdk iphonesimulator clang'
        as="$GAS_PREPROCESSOR -arch arm64 -- $cc"
        cflags="-mios-simulator-version-min=$DEPLOYMENT_TARGET"
        ;;

    simulator-x86_64)
        cpu='x86-64'
        cross_arch='x86_64'
        cc='xcrun -sdk iphonesimulator clang'
        as="$GAS_PREPROCESSOR -- $cc"
        cflags="-mios-simulator-version-min=$DEPLOYMENT_TARGET"
        ;;
esac

FFMPEG_FLAGS+=(
    --enable-pic
    --enable-videotoolbox
    --enable-hwaccel=h264_videotoolbox
    --enable-hwaccel=hevc_videotoolbox
    --enable-hwaccel=vp9_videotoolbox

    --enable-cross-compile
    --target-os=darwin
    --cpu=$cpu
    --arch=$cross_arch
    --cc="$cc"
    --as="$as"
    --extra-cflags="-arch $cross_arch $cflags"
    --extra-ldflags="-arch $cross_arch $cflags"
)

pushd . > /dev/null
prep_ffmpeg "iOS-$arch"
build_ffmpeg
popd > /dev/null

# Remove symlinks, keep only libraries with full version in their name
find "iOS-$arch" -type l -delete

