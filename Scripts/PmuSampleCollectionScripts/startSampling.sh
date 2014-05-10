# Script for running the PMU reader service via adb
# The setupDevice.sh script must be executed prior this executing this script.

#Port/Ip Args  - Where the PMU samples will be sent
ip='192.168.11.23'
port='5000'

#Args for each PMU event
period=25000
#Note: The PMU event numbers are in HEX
pmu0=0x06
pmu1=0x07
pmu2=0x0c
pmu3=0x0d
pmu4=0x0f
pmu5=0x12

#Convert PMU events to decimal
printf -v pmu0 "%d" $pmu0
printf -v pmu1 "%d" $pmu1
printf -v pmu2 "%d" $pmu2
printf -v pmu3 "%d" $pmu3
printf -v pmu4 "%d" $pmu4
printf -v pmu5 "%d" $pmu5

serviceName=com.uah.pmureader/com.uah.pmureader.PmuReaderService

adb shell am startservice -n $serviceName --es "ip" $ip --es "port" $port --ei "period" $period --ei "pmuEvent0" $pmu0 --ei "pmuEvent1" $pmu1 --ei "pmuEvent2" $pmu2 --ei "pmuEvent3" $pmu3 --ei "pmuEvent4" $pmu4 --ei "pmuEvent5" $pmu5



