#!/bin/sh

mkdir -p artifacts

lipo -create osx-arm64/libavcodec.58.dylib osx-x86_64/libavcodec.58.dylib -output artifacts/libavcodec.58.dylib
lipo -create osx-arm64/libavdevice.58.dylib osx-x86_64/libavdevice.58.dylib -output artifacts/libavdevice.58.dylib
lipo -create osx-arm64/libavfilter.7.dylib osx-x86_64/libavfilter.7.dylib -output artifacts/libavfilter.7.dylib
lipo -create osx-arm64/libavformat.58.dylib osx-x86_64/libavformat.58.dylib -output artifacts/libavformat.58.dylib
lipo -create osx-arm64/libavutil.56.dylib osx-x86_64/libavutil.56.dylib -output artifacts/libavutil.56.dylib
lipo -create osx-arm64/libswresample.3.dylib osx-x86_64/libswresample.3.dylib -output artifacts/libswresample.3.dylib
lipo -create osx-arm64/libswscale.5.dylib osx-x86_64/libswscale.5.dylib -output artifacts/libswscale.5.dylib
