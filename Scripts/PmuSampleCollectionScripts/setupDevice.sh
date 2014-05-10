# Setup script.  To be run after the device is flashed.
# This script performs the following actions:
#	Flashes device with modified kernel
#	Loads and sets up kernel module that performs PMU sampling
#	Installs the PMU reader application and services
#	Configures WiFi according the the SSID and Key variables
#	Installs applications in the directory argument that is passed in


#Verify input params
appToInstall=$1

#Configurable variables
ssid='stinet3'
key="useDefault"

#Setup ADB
echo '----------Setting up ADB'
cd android_source/
. build/envsetup.sh
#setpaths


#Flash modified kernel
echo '-----Flashing device with modified kernel'
cd /home/hunter/android_source/out/target/product/grouper
adb reboot bootloader
fastboot flash:raw boot zImage ramdisk.img 
fastboot continue

echo '-----Allowing for device to boot'
sleep 25

# Might need absolute paths for this to work!
if [[ $appToInstall != "" ]]; then
	if [[ -d $appToInstall ]]; then
		echo "----------Installing all applications in $appToInstall"
		FILES="$(find $appToInstall -type f -name '*.apk')"
		for f in $FILES
		do
			adb install $f
		done
	elif [[ -f $appToInstall ]]; then
		#Note: No need to copy library files.  They are installed as part of the APK.
	    	echo "----------Installing $appToInstall"
	    	adb install $appToInstall
	else
	    	echo "Error: $appToInstall is not valid file or directory"
	fi	
fi

#Push kernel module to device and do setup
echo '----------Loading kernel module'
cd /home/hunter/android_source/kernel/tegra/drivers/pmu_sync_sample
adb shell mkdir /data/modules
adb push pmu_sync_sample.ko /data/modules/
adb shell rmmod pmu_sync_sample
adb shell insmod /data/modules/pmu_sync_sample.ko
adb shell lsmod
echo 'Setting permissions on sysfs and char dev entries'
adb shell chmod -R 777 sys/sync_pmu/ 
adb shell chmod 777 dev/pmu_samples

#Install PMU reader app and services
echo '----------Installing PMU Reader App'
adb install /home/hunter/AndroidPmuReaderApp/bin/MainActivity.apk

#Wifi setup
echo 'Starting WiFi'
wifiServiceName=com.uah.pmureader/com.uah.pmureader.WifiService
adb shell am startservice -n $wifiServiceName --es "ssid" $ssid --es "pass" $key

cd ~

#Note: Can use 'aapt dump badging yourpkg.apk' to get the package name of an app.  Would have to parse the output
#TODO: Start up TCP server with specified file name from command line (or generate file name from app that is installed?)
$SHELL
