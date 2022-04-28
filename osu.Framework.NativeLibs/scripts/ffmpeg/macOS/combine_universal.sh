#!/bin/sh

mkdir -p macOS-universal

lipo -create macOS-arm64/libavcodec.58.dylib macOS-x86_64/libavcodec.58.dylib -output macOS-universal/libavcodec.58.dylib
lipo -create macOS-arm64/libavdevice.58.dylib macOS-x86_64/libavdevice.58.dylib -output macOS-universal/libavdevice.58.dylib
lipo -create macOS-arm64/libavfilter.7.dylib macOS-x86_64/libavfilter.7.dylib -output macOS-universal/libavfilter.7.dylib
lipo -create macOS-arm64/libavformat.58.dylib macOS-x86_64/libavformat.58.dylib -output macOS-universal/libavformat.58.dylib
lipo -create macOS-arm64/libavutil.56.dylib macOS-x86_64/libavutil.56.dylib -output macOS-universal/libavutil.56.dylib
lipo -create macOS-arm64/libswresample.3.dylib macOS-x86_64/libswresample.3.dylib -output macOS-universal/libswresample.3.dylib
lipo -create macOS-arm64/libswscale.5.dylib macOS-x86_64/libswscale.5.dylib -output macOS-universal/libswscale.5.dylib
