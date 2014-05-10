package com.uah.pmureader;

import android.app.Service;
import android.content.Intent;
import android.os.IBinder;

public class StopPmuReaderService extends Service 
{
	@Override
	public IBinder onBind(Intent intent) 
	{
		return null;
	}
	
	// Sends intent to stop the PMU reader service
	@Override
	public void onCreate() 
	{
		super.onCreate();
		Intent intent = new Intent(getBaseContext(), PmuReaderService.class);
		stopService(intent);
		stopSelf();
	}

}
