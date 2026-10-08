#!/bin/bash
set -Eeuo pipefail

# ensure we're running from the correct directory (location of this file).
cd "$(dirname "$0")"

# bass
echo "Downloading bass24.zip"
curl --show-error -Lsfo bass.zip https://www.un4seen.com/files/bass24.zip
unzip -qjo bass.zip x64/bass.dll -d runtimes/win-x64/native/
unzip -qjo bass.zip bass.dll -d runtimes/win-x86/native/

echo "Downloading bass24-arm64.zip"
curl --show-error -Lsfo bass24-arm64.zip https://www.un4seen.com/files/bass24-arm64.zip
unzip -qjo bass24-arm64.zip arm64/bass.dll -d runtimes/win-arm64/native/

echo "Downloading bass24-linux.zip"
curl --show-error -Lsfo bass-linux.zip https://www.un4seen.com/files/bass24-linux.zip
unzip -qjo bass-linux.zip libs/aarch64/libbass.so -d runtimes/linux-arm64/native/
unzip -qjo bass-linux.zip libs/x86/libbass.so -d runtimes/linux-x86/native/
unzip -qjo bass-linux.zip libs/x86_64/libbass.so -d runtimes/linux-x64/native/

echo "Downloading bass24-osx.zip"
curl --show-error -Lsfo bass-osx.zip https://www.un4seen.com/files/bass24-osx.zip
unzip -qjo bass-osx.zip libbass.dylib -d runtimes/osx/native/

echo "Downloading bass-ios.zip"
curl --show-error -Lsfo bass24-ios.zip https://www.un4seen.com/files/bass24-ios.zip
unzip -qo bass24-ios.zip bass.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

echo "Downloading bass-android.zip"
curl --show-error -Lsfo bass24-android.zip https://www.un4seen.com/files/bass24-android.zip
unzip -qjo bass24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -qjo bass24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -qjo bass24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/

# bassfx
echo "Downloading bass_fx.zip"
curl --show-error -Lsfo bass_fx.zip https://www.un4seen.com/files/z/0/bass_fx24.zip
unzip -qjo bass_fx.zip x64/bass_fx.dll -d runtimes/win-x64/native/
unzip -qjo bass_fx.zip bass_fx.dll -d runtimes/win-x86/native/

echo "Downloading bass_fx-arm64.zip"
curl --show-error -Lsfo bass_fx-arm64.zip https://www.un4seen.com/files/z/0/bass_fx24-arm64.zip
unzip -qjo bass_fx-arm64.zip arm64/bass_fx.dll -d runtimes/win-arm64/native/

echo "Downloading bass_fx-linux.zip"
curl --show-error -Lsfo bass_fx-linux.zip https://www.un4seen.com/files/z/0/bass_fx24-linux.zip
unzip -qjo bass_fx-linux.zip libs/aarch64/libbass_fx.so -d runtimes/linux-arm64/native/
unzip -qjo bass_fx-linux.zip libs/x86/libbass_fx.so -d runtimes/linux-x86/native/
unzip -qjo bass_fx-linux.zip libs/x86_64/libbass_fx.so -d runtimes/linux-x64/native/

echo "Downloading bass_fx-osx.zip"
curl --show-error -Lsfo bass_fx-osx.zip https://www.un4seen.com/files/z/0/bass_fx24-osx.zip
unzip -qjo bass_fx-osx.zip libbass_fx.dylib -d runtimes/osx/native/

echo "Downloading bass_fx24-ios.zip"
curl --show-error -Lsfo bass_fx24-ios.zip https://www.un4seen.com/files/z/0/bass_fx24-ios.zip
unzip -qo bass_fx24-ios.zip bass_fx.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

echo "Downloading bass_fx24-android.zip"
curl --show-error -Lsfo bass_fx24-android.zip https://www.un4seen.com/files/z/0/bass_fx24-android.zip
unzip -qjo bass_fx24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -qjo bass_fx24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -qjo bass_fx24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/


# bassmix
echo "Downloading bassmix.zip"
curl --show-error -Lsfo bassmix24.zip https://www.un4seen.com/files/bassmix24.zip
unzip -qjo bassmix24.zip x64/bassmix.dll -d runtimes/win-x64/native/
unzip -qjo bassmix24.zip bassmix.dll -d runtimes/win-x86/native/

unzip -qjo bass24-arm64.zip arm64/bassmix.dll -d runtimes/win-arm64/native/

echo "Downloading bassmix-linux.zip"
curl --show-error -Lsfo bassmix24-linux.zip https://www.un4seen.com/files/bassmix24-linux.zip
unzip -qjo bassmix24-linux.zip libs/aarch64/libbassmix.so -d runtimes/linux-arm64/native/
unzip -qjo bassmix24-linux.zip libs/x86/libbassmix.so -d runtimes/linux-x86/native/
unzip -qjo bassmix24-linux.zip libs/x86_64/libbassmix.so -d runtimes/linux-x64/native/

echo "Downloading bassmix-osx.zip"
curl --show-error -Lsfo bassmix24-osx.zip https://www.un4seen.com/files/bassmix24-osx.zip
unzip -qjo bassmix24-osx.zip libbassmix.dylib -d runtimes/osx/native/

echo "Downloading bassmix24-ios.zip"
curl --show-error -Lsfo bassmix24-ios.zip https://www.un4seen.com/files/bassmix24-ios.zip
unzip -qo bassmix24-ios.zip bassmix.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

echo "Downloading bassmix24-android.zip"
curl --show-error -Lsfo bassmix24-android.zip https://www.un4seen.com/files/bassmix24-android.zip
unzip -qjo bassmix24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -qjo bassmix24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -qjo bassmix24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/

# clean up
rm bass.zip
rm bass*.zip
