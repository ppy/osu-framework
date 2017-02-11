# osu-framework [![Build status](https://ci.appveyor.com/api/projects/status/fh5mnml3vsfheymp?svg=true)](https://ci.appveyor.com/project/peppy/osu-framework)
[dev chat](https://discord.gg/ppy)

A game framework written with osu! in mind.

# Requirements

- A desktop platform which can compile .NET 4.5.
- Visual Studio 2015 (community or otherwise) is recommended.

# Objectives

This framework is intended to take steps beyond what you would normally expect from a game framework. This means things like basic UI elements, text rendering, advanced input handling (textboxes) and performance overlays are provided out-of-the-box. Any of the osu! code that is deemed useful to other game projects will live in this framework project.

- Anywhere we implement graphical components, they will be displayed with a generic design and will be derivable for further customisation.
- Common elements used by games (texture caching, font loading) will be automatically initialised at runtime.

# Contributing

Contributions can be made via pull requests to this repository. We hope to credit and reward larger contributions via a [bounty system](https://goo.gl/nFdoyI). If you're unsure of what you can help with, check out the [list](https://github.com/ppy/osu-framework/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+label%3Abounty) of available issues with bounty.

Note that while we already have certain standards in place, nothing is set in stone. If you have an issue with the way code is structured; with any libraries we are using; with any processes involved with contributing, *please* bring it up. I welcome all feedback so we can make contributing to this project as pain-free as possible.

# Licence

This framework is licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Note that the BASS audio library (a dependency of this framework) is a commercial product. While it is free for non-commercial use, please ensure to [obtain a valid licence](http://www.un4seen.com/bass.html#license) if you plan on distributing any application using it commercially.
