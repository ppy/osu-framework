#!/bin/zsh

set -eu

pushd . > /dev/null

git clone "https://github.com/KhronosGroup/MoltenVK.git" "mvk-build"
cd "mvk-build"

./fetchDependencies --macos -v

make macos \
    MVK_CONFIG_LOG_LEVEL=1 \
    MVK_CONFIG_USE_METAL_ARGUMENT_BUFFERS=1 \
    MVK_CONFIG_SHOULD_MAXIMIZE_CONCURRENT_COMPILATION=1 \
    MVK_CONFIG_API_VERSION_TO_ADVERTISE=13

mkdir -p ../macOS-universal
cp "Package/Release/MoltenVK/dylib/macOS/libMoltenVK.dylib" "../macOS-universal/libMoltenVK.dylib"

popd > /dev/null
