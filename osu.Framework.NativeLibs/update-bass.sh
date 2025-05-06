#!/bin/bash

curl -Lso bass.zip https://www.un4seen.com/stuff/bass.zip
unzip -jo bass.zip x64/bass.dll -d runtimes/win-x64/native/
unzip -jo bass.zip bass.dll -d runtimes/win-x86/native/

curl -Lso bass-linux.zip https://www.un4seen.com/stuff/bass-linux.zip
unzip -jo bass-linux.zip x86/libbass.so -d runtimes/linux-x86/native/
unzip -jo bass-linux.zip x86_64/libbass.so -d runtimes/linux-x64/native/

curl -Lso bass-osx.zip https://www.un4seen.com/stuff/bass-osx.zip
unzip -jo bass-osx.zip libbass.dylib -d runtimes/osx/native/

curl -Lso bass24-ios.zip https://www.un4seen.com/files/bass24-ios.zip
unzip -o bass24-ios.zip bass.xcframework/* -d ../osu.Framework.iOS/runtimes/ios/native/

curl -Lso bass24-android.zip https://www.un4seen.com/files/bass24-android.zip
unzip -jo bass24-android.zip libs/arm64-v8a/* -d ../osu.Framework.Android/arm64-v8a/
unzip -jo bass24-android.zip libs/armeabi-v7a/* -d ../osu.Framework.Android/armeabi-v7a/
unzip -jo bass24-android.zip libs/x86/* -d ../osu.Framework.Android/x86/

rm bass*.zip
