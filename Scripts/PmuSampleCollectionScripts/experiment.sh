#!/bin/bash
# Invoked as ./experiment.sh <experiment #> <apk folder>
# Examples:
# ./experiment.sh 1 Malware_Apps_Test/FakeAngry
# ./experiment.sh 2 Good_Android_Apps_Train/
#
# Runs a PMU collection session for a Nexus 7 (2012 model).  This is the main script used when collecting samples.
# 
# NOTE:  Script was executed on a 32-bit Ubuntu 12.04 machine.  You will have to modify this file
# to point to the correct location of certain tools and files on your system. 
# Addtionally, the PMU events, IP address, and ports are set in the script below.

echo "---------------------------------------------------"
echo "- WARNING!                                        -"
echo "- PMU Codes are set for Experiment 2              -"
echo "---------------------------------------------------"

appsDir=`echo $2 | cut -d "/" -f2`

# Parse command line
case `echo $2 | cut -d "/" -f1` in
MalwareApps_Train)
	outputDir="Malware_Train"
	;;
MalwareApps_Test)
	outputDir="Malware_Test"
	;;
Good_Android_Apps_Train)
	outputDir="Ham_Train"
	appsDir="Good"
	;;
Good_Android_Apps_Test)
	outputDir="Ham_Test"
	appsDir="Good"
	;;
*)
	echo "Invalid app folder!"
	exit
	;;
esac

appToInstall=$2

echo '--------------Flashing device'
# Set path to compiled Android OS image.  Note that a custom kernel is needed to collect PMU samples.
# The build of Android (and kernel) is available at https://sites.google.com/a/uah.edu/uahuntsville-software-safety-and-security-laboratory/projects/android-security,
# along with the raw PMU samples that were collected.
export ANDROID_PRODUCT_OUT=/home/tlc0018/Android/CompiledAndroidOS_4_2_1/  
adb reboot bootloader

#It seems to work fine with out a delay.
#read -n1 -t10 -r -s -p $'---When the device enters fastboot mode,\n---press any key to continue.\n---(Or wait 10 seconds)\n' key

# Set path to Android SDK platform tools here
sudo -E /home/tlc0018/Android/sdk/platform-tools/fastboot -w flashall

cd ~

# wait for initial boot, and then play a sound.
echo "---Waiting for device to boot"
sleep 52
paplay /usr/share/sounds/ubuntu/stereo/bell.ogg &

read -n1 -r -s -p $'---Go through intial setup on the device.\n---When you have reached the home screen press any key.\n' key

#Configurable variables
ssid='Enter SSID Here'
key="Enter WiFi Key Here"

#Device ID
deviceId='015d4a82453bfa0c'

#Flash modified kernel, set paths to kernel and SDK tools here
echo '-----Flashing device with modified kernel'
cd /home/tlc0018/Android/CompiledAndroidOS_4_2_1/
adb reboot bootloader
sudo /home/tlc0018/Android/sdk/platform-tools/fastboot flash:raw boot zImage ramdisk.img 
sudo /home/tlc0018/Android/sdk/platform-tools/fastboot continue

echo '-----Allowing for device to boot'
#This loop checks "adb devices" for the device every second until the device is found.
sleep 25

#Push kernel module to device and do setup
echo '----------Loading kernel module'
cd /home/tlc0018/Android/tegra_kernel/drivers/pmu_sync_sample
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
adb install /home/tlc0018/Android/Projects/AndroidPmuReaderApp/bin/MainActivity.apk

#Wifi setup
echo 'Starting WiFi'
wifiServiceName=com.uah.pmureader/com.uah.pmureader.WifiService
adb shell am startservice -n $wifiServiceName --es "ssid" $ssid --es "pass" $key

cd ~
if [[ $appToInstall != "" ]]; then
	if [[ -d $appToInstall ]]; then
		echo "----------Installing all applications in $appToInstall"
		# Play a sound to let me know that I may need to verify app installs
		paplay /usr/share/sounds/ubuntu/stereo/bell.ogg &
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

#Note: Can use 'aapt dump badging yourpkg.apk' to get the package name of an app.  Would have to parse the output
#TODO: Start up TCP server with specified file name from command line (or generate file name from app that is installed?)

# START SERVER
cd ~

if [ -f server.socket ];
then
	rm server.socket
fi
touch server.socket

if [[ `ps aux | grep sampleServer | grep -v grep` != "" ]]
then
	echo "Rogue sampleServer process found, running \"killall sampleServer\""
	killall sampleServer
fi

if [ -f Experiment_$1/$outputDir/$appsDir.bin ]
then
	echo "---Experiment_$1/$outputDir/$appsDir.bin already exists!"
	# Prompt user
	read -p "---Would you like to delete this file and continue the script? (y/n): " -n 1 -r
	if [[ $REPLY =~ ^[Yy]$ ]]
	then
		rm Experiment_$1/$outputDir/$appsDir.bin
		echo ""
	else
		exit
	fi
fi

echo "---Starting server"
tail -f server.socket | ./server "Experiment_$1/$outputDir/$appsDir.bin" 5006 &
#tailPID=$!
echo "---Server started"

# START SAMPLING
read -n1 -r -s -p $'---Press any key to start sampling.\n' key

# Script for running the PMU reader service via adb
# The setupDevice.sh script must be executed prior this executing this script.

#Port/Ip Args  - Where the PMU samples will be sent
ip='192.168.1.119'
port='5006'

#Args for each PMU event
period=25000
#Note: The PMU event numbers are in HEX
pmu0=0x10
pmu1=0x02
pmu2=0x04
pmu3=0x6e
pmu4=0x01
pmu5=0x03

#Convert PMU events to decimal
printf -v pmu0 "%d" $pmu0
printf -v pmu1 "%d" $pmu1
printf -v pmu2 "%d" $pmu2
printf -v pmu3 "%d" $pmu3
printf -v pmu4 "%d" $pmu4
printf -v pmu5 "%d" $pmu5

serviceName=com.uah.pmureader/com.uah.pmureader.PmuReaderService

adb shell am startservice -n $serviceName --es "ip" $ip --es "port" $port --ei "period" $period --ei "pmuEvent0" $pmu0 --ei "pmuEvent1" $pmu1 --ei "pmuEvent2" $pmu2 --ei "pmuEvent3" $pmu3 --ei "pmuEvent4" $pmu4 --ei "pmuEvent5" $pmu5

# STOP SAMPLING
read -n1 -r -s -p $'---Press any key to stop sampling.\n' key
echo '---Stopping PMU reader service'
serviceName=com.uah.pmureader/com.uah.pmureader.StopPmuReaderService
adb shell am startservice -n $serviceName

# KILL SERVER
echo "---Stoping server"
echo "\n" > server.socket
#kill $tailPID
# TODO: This isn't really safe, but it works
killall tail
echo "---Server stopped."

echo "Disk usage of sampled file is:"
du -h Experiment_$1/$outputDir/$appsDir.bin

