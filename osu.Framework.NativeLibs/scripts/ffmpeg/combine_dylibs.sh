#!/bin/bash
set -eu

if [ -z "${platform-}" ]; then
    PS3='Combine binaries for which platform? '
    select platform in "macOS" "iOS"; do
        if [ -z "$platform" ]; then
            echo "invalid option"
        else
            break
        fi
    done
fi

pushd . > /dev/null
mkdir -p $platform-universal
cd $platform-arm64
for lib_arm in *.dylib; do
    lib_x86="../$platform-x86_64/$lib_arm"
    
    echo "-> Creating universal $lib_arm..."
    lipo -create "$lib_arm" "$lib_x86" -output "../$platform-universal/$lib_arm"
done
popd > /dev/null
