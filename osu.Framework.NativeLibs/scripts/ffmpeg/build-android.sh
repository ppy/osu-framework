#!/bin/bash
set -eu

# Android ABI level to target. 21 is the minimum supported by the NDK
# See https://apilevels.com for info on what the API level means
API_LEVEL="21"

if [ -z "${ANDROID_NDK_ROOT:-}" ]; then
  echo "ANDROID_NDK_ROOT must be set"
  exit 1
fi

pushd "$(dirname "$0")" > /dev/null
SCRIPT_PATH=$(pwd)
popd > /dev/null
source "$SCRIPT_PATH/common.sh"

if [ -z "${arch-}" ]; then
    PS3='Build for which arch? '
    select arch in "armeabi-v7a" "arm64-v8a" "x86" "x86_64"; do
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
cflags=''
asm_options=''

case $arch in
    armeabi-v7a)
        cpu='armv7-a'
        cross_arch='armv7-a'
        cc="armv7a-linux-androideabi${API_LEVEL}-clang"
        cflags='-mfpu=neon -mfloat-abi=softfp'
        asm_options='--enable-neon --enable-asm --enable-inline-asm'
        ;;

    arm64-v8a)
        cpu='armv8-a'
        cross_arch='aarch64'
        cc="aarch64-linux-android${API_LEVEL}-clang"
        asm_options='--enable-neon --enable-asm --enable-inline-asm'
        ;;

    x86)
        cpu='i686'
        cross_arch='i686'
        cc="i686-linux-android${API_LEVEL}-clang"
        # ASM has text relocations
        asm_options='--disable-asm'
        ;;

    x86_64)
        cpu='x86-64'
        cross_arch='x86_64'
        cc="x86_64-linux-android${API_LEVEL}-clang"
        asm_options='--enable-asm --enable-inline-asm'
        ;;
esac

toolchain_path="$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64"
bin_path="$toolchain_path/bin"

FFMPEG_FLAGS+=(
    --enable-jni

    --enable-cross-compile
    --target-os=android
    --cpu=$cpu
    --arch=$cross_arch
    --sysroot="$toolchain_path/sysroot"
    --cc="$bin_path/$cc"
    --cxx="$bin_path/$cc++"
    --ld="$bin_path/$cc"
    --ar="$bin_path/llvm-ar"
    --as="$bin_path/$cc"
    --nm="$bin_path/llvm-nm"
    --ranlib="$bin_path/llvm-ranlib"
    --strip="$bin_path/llvm-strip"
    --x86asmexe="$bin_path/yasm"
    --extra-cflags="-fstrict-aliasing -fPIC -DANDROID -D__ANDROID__ $cflags"

    $asm_options
)

pushd . > /dev/null
prep_ffmpeg "android-$arch"
build_ffmpeg
popd > /dev/null

