# osu.Framework.Templates

Templates to use when starting off with osu!framework. Create a fully-testable, cross-platform, and ready-for-git project using osu!framework in just two lines.

## Usage

```bash
# install (or update) template package.
# this only needs to be done once
dotnet new -i ppy.osu.Framework.Templates

## IMPORTANT: Do not use spaces or hyphens in your project name for the following commands.
## This does not play nice with the templating system.

# create a new empty freeform osu!framework game
dotnet new osu-framework-game -n MyNewGame

# ...or start with a working Flappy Bird clone
dotnet new osu-framework-flappy-game -n MyNewFlappyGame
```
