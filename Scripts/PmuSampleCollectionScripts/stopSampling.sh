# Stops the PMU reader service, which in turn stops collecting PMU samples

echo 'Stopping PMU reader service'
serviceName=com.uah.pmureader/com.uah.pmureader.StopPmuReaderService
adb shell am startservice -n $serviceName
