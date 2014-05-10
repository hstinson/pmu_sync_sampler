set -e
make clean
make

adb push pmu_sync_sample.ko /data/modules/

#echo 'Disabling auto hotplug and enabling all CPUs'
#adb shell "echo 0 > /sys/module/cpu_tegra3/parameters/auto_hotplug"
#for i in 0 1 2 3; do adb shell "echo 1 > /sys/devices/system/cpu/cpu${i}/online" ; done

echo 'Loading module'
adb shell rmmod pmu_sync_sample
adb shell insmod /data/modules/pmu_sync_sample.ko
adb shell lsmod
echo 'Setting permissions on sysfs and char dev entries'
adb shell chmod -R 777 sys/sync_pmu/ 
adb shell chmod 777 dev/pmu_samples

$SHELL
