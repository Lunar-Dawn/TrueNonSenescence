#!/usr/bin/env bash

if ! [[ -d "$1" ]]; then
	echo "Export directory $1 does not exist. Aborting."
	exit 1
fi

DIR="$1/TrueNonSenescence"
echo $DIR

if [[ -d $DIR ]]; then
	rm -r $DIR
fi

mkdir $DIR
cp -r ./About $DIR
cp -r ./1.6 $DIR
cp    ./loadFolders.xml $DIR
