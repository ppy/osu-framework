#!/bin/bash

DEADLOCK_TIME=3600

UPTIME=$(uptime -p)
echo "Uptime: $UPTIME"

find . -type d -name "bin" -print0 | xargs -0 rm -rf
find . -type d -name "obj" -print0 | xargs -0 rm -rf

rm -rf inspectcode
dotnet tool restore

dotnet tool install -g dotnet-dump

strace -s 2000 dotnet jb inspectcode $(pwd)/osu.Framework.NativeLibs/osu.Framework.NativeLibs.csproj --no-build --debug --loglevel=TRACE --logfile=$(pwd)/inspectcode.log --output=$(pwd)/inspectcodereport.xml --cachesDir=$(pwd)/inspectcode &

echo "Waiting for R# CLI"
sleep 10

PID=$(ps axf | grep 'dotnet exec' | grep -v grep | awk '{print $1}')
echo "R# PID: $PID"

echo "Waiting $DEADLOCK_TIME seconds for deadlock"
sleep $DEADLOCK_TIME

if ps -p $PID > /dev/null; then
    echo "Deadlocked, dumping..."
    dotnet dump collect -p $PID -o deadlock.dmp

    echo "Compressing dump..."
    tar -cjSf deadlock.tar.bz2 deadlock.dmp

    echo "Killing R# process"
    kill $PID
fi

echo "Compressing log..."
tar -cjSf inspectcode.log.tar.bz2 $(pwd)/inspectcode.log
