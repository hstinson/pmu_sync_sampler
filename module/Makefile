export ARCH=arm
export SUBARCH=arm
export CROSS_COMPILE=arm-linux-androideabi-
export PATH:=/home/hunter/android_source/prebuilts/gcc/linux-x86/arm/arm-eabi-4.6/bin/arm-eabi-:$(PATH)

ccflags-y = -mtune=cortex-a9 -mcpu=cortex-a9 -fno-pic #-mno-unaligned-access
obj-m += pmu_sync_sample.o 

pmu_sync_sample-objs := pmu_sync_sample_main.o v7_pmu.o arm.o

all:
	make -C /home/hunter/android_source/kernel/tegra M=$(PWD) modules

clean:
	make -C /home/hunter/android_source/kernel/tegra M=$(PWD) clean

