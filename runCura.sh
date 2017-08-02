#!/bin/sh

HOME='/usr/share/polar3d'
CONFIGINI=$HOME'/working/config.ini'
SLICEDGCODE=$HOME'/working/working.gcode'
ORIGSTL=$HOME'/working/original.stl'

echo $(time nice -n -10 CuraEngine -v -m '.819152044,0,.573576436,0,1,0,-.573576436,0,.819152044' -c  $CONFIGINI -o $SLICEDGCODE  $ORIGSTL)

node /usr/share/polar3d/server/shift.js $HOME'/working/working.gcode' > $HOME/'working/sliced.gcode'

exit