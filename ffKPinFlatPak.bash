#!/usr/bin/env -S bash 
PROFILENAME='KPinFlatPak'
LAUNCHFOLDER="${HOME}/tmp/${PROFILENAME}.dir"
if [[ ! -d $LAUNCHFOLDER ]]; then mkdir -p "$LAUNCHFOLDER"; fi;
pushd "${LAUNCHFOLDER}"
echo 'HELP:  flatpak run org.mozilla.firefox --help '
echo 'ProfileManager: pass in --ProfileManager'
echo '--createprofile NewProfileName'
pwd

flatpak run --verbose org.mozilla.firefox -P ${PROFILENAME} $@ &

pushd "${LAUNCHFOLDER}"
echo "flatpak must want to change to the user's home folder"
echo "For iDrac, iLo, ipKVM, in ${HOME}/.config/icedtea-web/deployment.properties, set:"
echo "deployment.browser.path=$0"
echo
echo 'Search for \"Allocated instance id\" in this output to find the tab completeable first column (instanceID?) in `flatpak ps` output.'
echo '`flatpak --verbose ps --verbose --columns=all`'  
pushd "${LAUNCHFOLDER}"
