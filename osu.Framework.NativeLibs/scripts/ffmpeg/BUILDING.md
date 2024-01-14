# Build Instructions

1. Install the dependencies for your platform(s)
2. Run the build script for your platform(s)
3. (macOS only) Run `combine_dylibs.sh` to create universal dylibs

## Build dependencies

In general, you need `gcc`, `make`, and `nasm`.
If external libraries need to be included in the build, `pkg-config` is also required.

### Windows Dependencies (compiling on Ubuntu/Debian)

Targetting x86 and x86_64:

```sh
sudo apt install make nasm gcc mingw-w64 mingw-w64-tools
```

Targetting aarch64:

```sh
sudo apt install make nasm

# Downloading a llvm-mingw release and adding it to PATH #
# If host is x86_64:
url="https://github.com/mstorsjo/llvm-mingw/releases/download/20230614/llvm-mingw-20230614-ucrt-ubuntu-20.04-x86_64.tar.xz"
# If host is aarch64:
#url="https://github.com/mstorsjo/llvm-mingw/releases/download/20230614/llvm-mingw-20230614-ucrt-ubuntu-20.04-aarch64.tar.xz"
curl -Lo llvm-mingw.tar.xz "$url"
mkdir llvm-mingw
tar xfJ llvm-mingw.tar.xz --strip 1 -C llvm-mingw
export PATH="$PATH:$PWD/llvm-mingw/bin"
```

### macOS Dependencies

Check [this page](https://trac.ffmpeg.org/wiki/CompilationGuide/macOS#CompilingFFmpegyourself) for instructions on how to install macOS dependencies.
Note that you don't need packages like `x264` or `libvpx`, it is enough to install these packages in addition to Xcode:

```zsh
brew install gcc make nasm
```

### Linux Dependencies (Ubuntu/Debian)

```sh
sudo apt install make nasm gcc pkg-config libva-dev libvdpau-dev
```

## Output files

For each `<platform>-<arch>` combination, two directories will be created. The directory called `<platform>-<arch>` will
contain the resulting shared libraries, and the directory with a `-build` suffix will contain the respective build files.

### macOS only: Combine arch-specific dylib files into universal dylib files

The `combine_universal.sh` script will combine the `x86_64` and `arm64` dylibs into universal dylibs.
The universal dylibs are output into a folder named `macOS-universal` and should be copied into `osu.Framework.NativeLibs/runtimes/osx`.

## Cleanup

Files are left around for debugging purposes, manually delete the directories to clean up.