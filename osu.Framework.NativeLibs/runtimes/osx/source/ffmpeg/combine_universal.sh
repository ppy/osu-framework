#!/bin/sh

lipo -create build-arm64/libavcodec.58.dylib build-x86_64/libavcodec.58.dylib -output ../../native/libavcodec.58.dylib
lipo -create build-arm64/libavdevice.58.dylib build-x86_64/libavdevice.58.dylib -output ../../native/libavdevice.58.dylib
lipo -create build-arm64/libavfilter.7.dylib build-x86_64/libavfilter.7.dylib -output ../../native/libavfilter.7.dylib
lipo -create build-arm64/libavformat.58.dylib build-x86_64/libavformat.58.dylib -output ../../native/libavformat.58.dylib
lipo -create build-arm64/libavutil.56.dylib build-x86_64/libavutil.56.dylib -output ../../native/libavutil.56.dylib
lipo -create build-arm64/libswresample.3.dylib build-x86_64/libswresample.3.dylib -output ../../native/libswresample.3.dylib
lipo -create build-arm64/libswscale.5.dylib build-x86_64/libswscale.5.dylib -output ../../native/libswscale.5.dylib
