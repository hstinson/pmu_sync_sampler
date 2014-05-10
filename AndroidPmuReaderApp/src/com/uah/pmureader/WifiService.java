package com.uah.pmureader;

import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.net.wifi.WifiConfiguration;
import android.net.wifi.WifiManager;
import android.os.IBinder;
import android.util.Log;
import android.widget.Toast;

public class WifiService extends Service {

	@Override
	public IBinder onBind(Intent intent) 
	{
		return null;
	}
	
	@Override
	public int onStartCommand(Intent intent, int flags, int startId)
	{			
		String ssid = intent.getStringExtra("ssid");
		String pass = intent.getStringExtra("pass");
		
		if (ssid != null && pass != null)
		{
			Log.d("PMU", String.format("SSID: %s", ssid));
			Log.d("PMU", String.format("PSK: %s", pass));			
			
			configWifi(ssid, pass);
			Toast.makeText(this, "Starting WiFi...", Toast.LENGTH_LONG).show();
		}
		else
		{
			Toast.makeText(this, "Unable to setup and start WiFi", Toast.LENGTH_LONG).show();
		}
	
		return START_STICKY;
	}
	
	/**
	 * Configures and enables WiFi
	 * @param view
	 */
	private void configWifi(String ssid, String pass)
	{		
		// Reference: http://stackoverflow.com/questions/6141185/android-connect-to-wifi-without-human-interaction 
		WifiManager wifiManager = (WifiManager) getSystemService(Context.WIFI_SERVICE);
		wifiManager.setWifiEnabled(true);
		
        // setup a wifi configuration
        WifiConfiguration wc = new WifiConfiguration();
        wc.SSID = String.format("\"%s\"", ssid);
        wc.hiddenSSID = true;
        wc.allowedAuthAlgorithms.set(WifiConfiguration.AuthAlgorithm.OPEN);
        wc.preSharedKey = String.format("\"%s\"", pass);
        wc.status = WifiConfiguration.Status.ENABLED;
        wc.allowedGroupCiphers.set(WifiConfiguration.GroupCipher.TKIP);
        wc.allowedGroupCiphers.set(WifiConfiguration.GroupCipher.CCMP);
        wc.allowedKeyManagement.set(WifiConfiguration.KeyMgmt.WPA_PSK);
        wc.allowedPairwiseCiphers.set(WifiConfiguration.PairwiseCipher.TKIP);
        wc.allowedPairwiseCiphers.set(WifiConfiguration.PairwiseCipher.CCMP);
        wc.allowedProtocols.set(WifiConfiguration.Protocol.RSN);
        wc.allowedProtocols.set(WifiConfiguration.Protocol.WPA);
        // connect to and enable the connection
        int netId = wifiManager.addNetwork(wc);
        wifiManager.enableNetwork(netId, true);
	}
}
