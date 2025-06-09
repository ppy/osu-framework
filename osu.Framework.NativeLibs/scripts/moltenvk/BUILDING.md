# Build Instructions

NOTE: MoltenVK is supported only on Apple devices.

1. Install the dependencies
2. Run the build script `build-macOS.sh`

### Dependencies

Check [this page](https://github.com/KhronosGroup/MoltenVK) for instructions on how to install dependencies.
It is enough to install these packages in addition to Xcode:

```zsh
brew install git cmake python3 ninja make
```

## Output files

The shared library will be located in the `macOS-universal` directory,
and the directory called `mvk-build` will contain the respective build files.

## Cleanup

Files are left around for debugging purposes, manually delete the directories to clean up.