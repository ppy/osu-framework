# Script for generating mtouch flags to preserve symbols of all of the libraries included in $LIBRARIES.
# That is to avoid them from getting stripped out due to appearing falsely unused.

IOS_PROJECT=$(dirname $0)
FRAMEWORK=$(cd "$IOS_PROJECT/.."; pwd)

LIBRARIES=("$IOS_PROJECT/libbass_fx.a" "$IOS_PROJECT/libbassmix.a")
PROJECTS=("$FRAMEWORK/osu.Framework.iOS.props")
for library in ${LIBRARIES[@]}; do
    flags="$flags $(nm -g $library | grep -o "T .*" | sort -u | sed 's/T _/--nosymbolstrip=/' | tr '\n' ' ')"
    echo "Generated mtouch symbol strip flags for excluding '$(basename $library)'."
done

flags=$(echo "$flags" | xargs)

for project in ${PROJECTS[@]}; do
    sed -i '' "s~<GeneratedMtouchSymbolStripFlags>.*<~<GeneratedMtouchSymbolStripFlags>$flags<~" $project
done

echo "Updated framework projects."
