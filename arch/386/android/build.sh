#!/bin/bash

if [ ! -f build.xml ]
then
    android update project -p . -s --target android-10
fi

# takes as extra argument a directory where to look for .so-s

ENGINES="gforth-fast gforth-itc"

GFORTH_VERSION=$(gforth --version 2>&1 | cut -f2 -d' ')
APP_VERSION=$[$(cat ~/.app-version)+1]
echo $APP_VERSION >~/.app-version

sed -e "s/@VERSION@/$GFORTH_VERSION/g" -e "s/@APP@/$APP_VERSION/g" <AndroidManifest.xml.in >AndroidManifest.xml

SRC=../../..
LIBS=libs/x86
LIBCCNAMED=lib/$(gforth --version 2>&1 | tr ' ' '/')/libcc-named/.libs

rm -rf $LIBS
mkdir -p $LIBS

if [ "$1" != "--no-gforthgz" ]
then
    (cd $SRC
	if [ "$1" != "--no-config" ]; then ./configure --host=i686-linux-android --with-cross=android --with-ditc=gforth-ditc-x32 --prefix= --datarootdir=/sdcard --libdir=/sdcard --libexecdir=/lib --enable-lib || exit 1; fi
	make # || exit 1
	make setup-debdist || exit 1) || exit 1
    if [ "$1" == "--no-config" ]; then CONFIG=no; shift; fi

    for i in . $*
    do
	cp $i/*.{fs,fi,png,jpg} $SRC/debian/sdcard/gforth/site-forth
    done
    (cd $SRC/debian/sdcard
	mkdir -p gforth/home
	gforth ../../archive.fs gforth/home/ $(find gforth -type f)) | gzip -9 >$LIBS/libgforthgz.so
else
    shift
fi

SHA256=$(sha256sum libs/x86/libgforthgz.so | cut -f1 -d' ')

for i in $ENGINES
do
    sed -e "s/sha256sum-sha256sum-sha256sum-sha256sum-sha256sum-sha256sum-sha2/$SHA256/" $SRC/engine/.libs/lib$i.so >$LIBS/lib$i.so
done

ANDROID=${PWD%/*/*/*}
CFLAGS="-O3" 
LIBCC=$SRC
for i in $LIBCC $*
do
    (cd $i; test -d shlibs && \
	(cd shlibs
	    for j in *; do
		(cd $j
		    if [ "$CONFIG" == no ]
		    then
			make
		    else
			./configure CFLAGS="$CFLAGS" --host=i686-linux-android && make clean && make
		    fi
		)
	    done
	)
    )
    (cd $i; test -x ./libcc.android && ANDROID=$ANDROID ./libcc.android)
    for j in $LIBCCNAMED .libs
    do
	for k in $(cd $i/$j; echo *.so)
	do
	    cp $i/$j/$k $LIBS
	done
    done
    shift
done
strip $LIBS/*.so
#ant debug
ant release
cp bin/Gforth-release.apk bin/Gforth.apk
#jarsigner -verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore ~/.gnupg/bernd-release-key.keystore bin/Gforth$EXT.apk bernd
