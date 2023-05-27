game_projects=(./**/**/**/*.Game.csproj)
ios_projects=(./**/**/**/*.iOS.csproj)

cd "$(dirname "$0")"

for game_project in ${game_projects[@]}
do
    dotnet remove ${game_project} reference ../osu.Framework/osu.Framework.csproj
    dotnet add $game_project package ppy.osu.Framework -v $1 --no-restore
done

for ios_project in ${ios_projects[@]}
do
    dotnet remove $ios_project reference ../osu.Framework.iOS/osu.Framework.iOS.csproj
    dotnet add $ios_project package ppy.osu.Framework.iOS -v $1 --no-restore
done

sed -i '/osu.Framework.iOS.props/d' ./**/**/**/*.iOS.csproj
