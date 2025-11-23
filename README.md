<p align="center">
  <img width="500px" src="assets/o!f Logo Large FC.svg">
</p>

# osu!framework

[![Build status](https://github.com/ppy/osu-framework/actions/workflows/ci.yml/badge.svg?branch=master&event=push)](https://github.com/ppy/osu-framework/actions/workflows/ci.yml)
[![GitHub release](https://img.shields.io/github/release/ppy/osu-framework.svg)](https://github.com/ppy/osu-framework/releases/latest)
[![CodeFactor](https://www.codefactor.io/repository/github/ppy/osu-framework/badge)](https://www.codefactor.io/repository/github/ppy/osu-framework)
[![dev chat](https://discordapp.com/api/guilds/188630481301012481/widget.png?style=shield)](https://discord.gg/ppy)

A game framework written with [osu!](https://github.com/ppy/osu) in mind.

## Developing a game using osu!framework

If you are interested in **creating a project** using the framework, please start from the [getting started](https://github.com/ppy/osu-framework/wiki/Setting-up-your-first-project) wiki resources (or jump straight over to the [project templates](https://github.com/ppy/osu-framework/tree/master/osu.Framework.Templates)). You can either start off from an empty project, or take a peek at a working sample game. Either way, full project structure, cross-platform support, and  a testing setup are included!

The rest of the information on this page is related to working *on* the framework, not *using* it!

## Objectives

This framework is intended to take steps beyond what you would normally expect from a game framework. This means things like basic UI elements, text rendering, advanced input handling (textboxes) and performance overlays are provided out-of-the-box. Any of the osu! code that is deemed useful to other game projects will live in this framework project.

- Anywhere we implement graphical components, they will be displayed with a generic design and will be derivable for further customisation.
- Common elements used by games (texture caching, font loading) will be automatically initialised at runtime.
- Allow for isolated development of components via a solid testing environment (`VisualTests` and `TestCases`). Check the [wiki](https://github.com/ppy/osu-framework/wiki/Development-and-Testing) for more information on how these can be used to streamline development.

## Requirements

- A desktop platform with the [.NET 8.0 SDK](https://dotnet.microsoft.com/download).
- When running on linux, please have a system-wide ffmpeg installation available to support video decoding.
- When running on Windows 7 or 8.1, *[additional prerequisites](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60&pivots=os-windows#dependencies)** may be required to correctly run .NET 8 applications if your operating system is not up-to-date with the latest service packs.
- When working with the codebase, we recommend using an IDE with intellisense and syntax highlighting, such as [Visual Studio 2019+](https://visualstudio.microsoft.com/vs/), [Jetbrains Rider](https://www.jetbrains.com/rider/), or [Visual Studio Code](https://code.visualstudio.com/) with the [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) and [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) plugin installed.

### Building

Build configurations for the recommended IDEs (listed above) are included. You should use the provided Build/Run functionality of your IDE to get things going. When testing or building new components, it's highly encouraged you use the `VisualTests` project/configuration. More information on this provided [below](#contributing).

- Visual Studio / Rider users should load the project via one of the platform-specific .slnf files, rather than the main .sln. This will allow access to template run configurations.

### Code analysis

Code analysis can be run with `powershell ./InspectCode.ps1` or `InspectCode.sh`.

## Contributing

Contributions can be made via pull requests to this repository.

If you're unsure of what you can help with, check out the [list of open issues](https://github.com/ppy/osu-framework/issues) (especially those with the ["good first issue"](https://github.com/ppy/osu-framework/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3A%22good+first+issue%22) label).

Before starting, please make sure you are familiar with the [development and testing](https://github.com/ppy/osu-framework/wiki/Development-and-Testing) procedure we have set up. New component development, and where possible, bug fixing and debugging existing components **should always be done under VisualTests**.

Note that while we already have certain standards in place, nothing is set in stone. If you have an issue with the way code is structured; with any libraries we are using; with any processes involved with contributing, *please* bring it up. We welcome all feedback so we can make contributing to this project as pain-free as possible.

We love to reward quality contributions. If you have made a large contribution, or are a regular contributor, you are welcome to [submit an expense via opencollective](https://opencollective.com/ppy/expenses/new). If you have any questions, feel free to [reach out to peppy](mailto:pe@ppy.sh) before doing so.

## Licence

This framework is licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Portions of this software are copyright © 2025 The FreeType Project (https://freetype.org). All rights reserved.

The BASS audio library (a dependency of this framework) is a commercial product. While it is free for non-commercial use, please ensure to [obtain a valid licence](http://www.un4seen.com/bass.html#license) if you plan on distributing any application using it commercially.

## Projects that use osu!framework

[osu!](https://github.com/ppy/osu) – rhythm is just a *click* away!

[GDEdit](https://github.com/gd-edit/GDE) - A third-party Geometry Dash editor.

[Vignette](https://github.com/vignette-project/vignette) - An OpenCV-based facial recognition software for Live2D

[IWBTM](https://github.com/EVAST9919/iwbtm) - A platform game with level editor based off of "I Wanna..." games

[DeltaDash](https://deltada.sh/) - A multi-direction, lane-based scroller rhythm game

[fluXis](https://github.com/TeamFluXis/fluXis) - A community-driven rhythm game with a focus on creativity and expression

<!--
We love to see people using our framework! Add your project here via a PR!

Conditions:
 - Must be a GitHub link (i.e. your project is open source)
 - Must be actively developed (and have executable releases)
-->
