#!/bin/bash

# bass
curl -Lso bass.zip https://www.un4seen.com/stuff/bass.zip
unzip -qjo bass.zip x64/bass.dll -d runtimes/win-x64/native/
unzip -qjo bass.zip bass.dll -d runtimes/win-x86/native/

curl -Lso bass24-arm64.zip https://www.un4seen.com/files/bass24-arm64.zip
unzip -qjo bass24-arm64.zip arm64/bass.dll -d runtimes/win-arm64/native/

curl -Lso bass-linux.zip https://www.un4seen.com/stuff/bass-linux.zip
unzip -qjo bass-linux.zip x86/libbass.so -d runtimes/linux-x86/native/
unzip -qjo bass-linux.zip x86_64/libbass.so -d runtimes/linux-x64/native/

curl -Lso bass-osx.zip https://www.un4seen.com/stuff/bass-osx.zip
unzip -qjo bass-osx.zip libbass.dylib -d runtimes/osx/native/

curl -Lso bass24-ios.zip https://www.un4seen.com/files/bass24-ios.zip
unzip -qo bass24-ios.zip bass.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

curl -Lso bass24-android.zip https://www.un4seen.com/files/bass24-android.zip
unzip -qjo bass24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -qjo bass24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -qjo bass24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/

# bassfx
curl -Lso bass_fx.zip https://www.un4seen.com/stuff/bass_fx.zip
unzip -qjo bass_fx.zip x64/bass_fx.dll -d runtimes/win-x64/native/
unzip -qjo bass_fx.zip bass_fx.dll -d runtimes/win-x86/native/

curl -Lso bass_fx-arm64.zip https://www.un4seen.com/stuff/bass_fx-arm64.zip
unzip -qjo bass_fx-arm64.zip bass_fx.dll -d runtimes/win-arm64/native/

curl -Lso bass_fx-linux.zip https://www.un4seen.com/stuff/bass_fx-linux.zip
unzip -qjo bass_fx-linux.zip x86/libbass_fx.so -d runtimes/linux-x86/native/
unzip -qjo bass_fx-linux.zip x86_64/libbass_fx.so -d runtimes/linux-x64/native/

curl -Lso bass_fx-osx.zip https://www.un4seen.com/stuff/bass_fx-osx.zip
unzip -qjo bass_fx-osx.zip libbass_fx.dylib -d runtimes/osx/native/

curl -Lso bass_fx24-ios.zip https://www.un4seen.com/files/z/0/bass_fx24-ios.zip
unzip -qo bass_fx24-ios.zip bass_fx.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

curl -Lso bass_fx24-android.zip https://www.un4seen.com/files/z/0/bass_fx24-android.zip
unzip -qjo bass_fx24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -qjo bass_fx24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -qjo bass_fx24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/


# bassmix
curl -Lso bassmix24.zip https://www.un4seen.com/stuff/bassmix.zip
unzip -qjo bassmix24.zip x64/bassmix.dll -d runtimes/win-x64/native/
unzip -qjo bassmix24.zip bassmix.dll -d runtimes/win-x86/native/

unzip -qjo bass24-arm64.zip arm64/bassmix.dll -d runtimes/win-arm64/native/

curl -Lso bassmix24-linux.zip https://www.un4seen.com/stuff/bassmix-linux.zip
unzip -qjo bassmix24-linux.zip x86/libbassmix.so -d runtimes/linux-x86/native/
unzip -qjo bassmix24-linux.zip x86_64/libbassmix.so -d runtimes/linux-x64/native/

curl -Lso bassmix24-osx.zip https://www.un4seen.com/stuff/bassmix-osx.zip
unzip -qjo bassmix24-osx.zip libbassmix.dylib -d runtimes/osx/native/

curl -Lso bassmix24-ios.zip https://www.un4seen.com/files/bassmix24-ios.zip
unzip -qo bassmix24-ios.zip bassmix.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

curl -Lso bassmix24-android.zip https://www.un4seen.com/files/bassmix24-android.zip
unzip -qjo bassmix24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -qjo bassmix24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -qjo bassmix24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/

# clean up
rm bass*.zip
