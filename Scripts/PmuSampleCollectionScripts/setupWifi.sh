#Configurable variables
ssid='SSID Goes here'
key='WiFi key goes here'

#Wifi setup
echo 'Starting WiFi'
wifiServiceName=com.uah.pmureader/com.uah.pmureader.WifiService
adb shell am startservice -n $wifiServiceName --es "ssid" $ssid --es "pass" $key
