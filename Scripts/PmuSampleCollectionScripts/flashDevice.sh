# Flashes the device with the default kernel and OS

#Setup ADB
echo '----------Setting up ADB'
cd android_source/
. build/envsetup.sh
setpaths

echo '--------------Flashing device'
lunch full_grouper-eng
adb reboot bootloader

echo 'Alowing Device to Reboot...'
sleep 10
echo 'Done\n'

fastboot -w flashall

cd ~
