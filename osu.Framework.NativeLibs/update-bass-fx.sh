#!/bin/bash

curl -Lso bass_fx.zip https://www.un4seen.com/stuff/bass_fx.zip
unzip -jo bass_fx.zip x64/bass_fx.dll -d runtimes/win-x64/native/
unzip -jo bass_fx.zip bass_fx.dll -d runtimes/win-x86/native/

curl -Lso bass_fx-linux.zip https://www.un4seen.com/stuff/bass_fx-linux.zip
unzip -jo bass_fx-linux.zip x86/libbass_fx.so -d runtimes/linux-x86/native/
unzip -jo bass_fx-linux.zip x86_64/libbass_fx.so -d runtimes/linux-x64/native/

curl -Lso bass_fx-osx.zip https://www.un4seen.com/stuff/bass_fx-osx.zip
unzip -jo bass_fx-osx.zip libbass_fx.dylib -d runtimes/osx/native/

rm bass*.zip
