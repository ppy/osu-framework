#!/bin/bash
set -eu

pushd . > /dev/null
mkdir -p macOS-universal
cd macOS-arm64
for lib_arm in *.dylib; do
    lib_x86="../macOS-x86_64/$lib_arm"
    
    echo "-> Creating universal $lib_arm..."
    lipo -create "$lib_arm" "$lib_x86" -output "../macOS-universal/$lib_arm"
done
popd > /dev/null
