#!/bin/bash
set -eu

# See build-iOS.sh
DEPLOYMENT_TARGET="13.4"

for arch in "arm64" "simulator-universal"; do
    pushd . > /dev/null
    cd "iOS-$arch"
    for f in *.*.*.*.dylib; do
        [ -f "$f" ] || continue

        # [avcodec].58.10.72.dylib
        lib_name="${f%.*.*.*.*}"

        # avcodec.[58.10.72].dylib
        tmp=${f#*.}
        version_string="${tmp%.*}"

        framework_dir="$lib_name.framework"
        mkdir "$framework_dir"

        mv -v "$f" "$framework_dir/$lib_name"
        
        plist_file="$framework_dir/Info.plist"
        
        plutil -create xml1 "$plist_file"
        plutil -insert CFBundleDevelopmentRegion -string en "$plist_file"
        plutil -insert CFBundleExecutable -string "$lib_name" "$plist_file"
        plutil -insert CFBundleIdentifier -string "sh.ppy.osu.Framework.iOS.$lib_name" "$plist_file"
        plutil -insert CFBundleInfoDictionaryVersion -string '6.0' "$plist_file"
        plutil -insert CFBundleName -string "$lib_name" "$plist_file"
        plutil -insert CFBundlePackageType -string FMWK "$plist_file"
        plutil -insert CFBundleShortVersionString -string "$version_string" "$plist_file"
        plutil -insert CFBundleVersion -string "$version_string" "$plist_file"
        plutil -insert MinimumOSVersion -string "$DEPLOYMENT_TARGET" "$plist_file"
        plutil -insert CFBundleSupportedPlatforms -array "$plist_file"
        plutil -insert CFBundleSupportedPlatforms -string iPhoneOS -append "$plist_file"

    done
    popd > /dev/null
done

pushd . > /dev/null
mkdir -p iOS-xcframework
cd iOS-arm64
for framework_arm in *.framework; do
    xcodebuild -create-xcframework \
               -framework "$framework_arm" \
               -framework "../iOS-simulator-universal/$framework_arm" \
               -output "../iOS-xcframework/${framework_arm%.framework}.xcframework"
done
popd > /dev/null

