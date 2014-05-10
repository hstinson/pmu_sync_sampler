package com.uah.pmureader;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

import com.uah.pmureader.R;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Intent;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.IBinder;
import android.util.Log;
import android.widget.Toast;

/**
 * PMU reader service.
 * @author hunter
 */
public class PmuReaderService extends Service
{
	NativeLib lib;
	int period;
	int event0;
	int event1;
	int event2;
	int event3;
	int event4;
	int event5;
	String ip;
	String port;
	
	private static final Class<?>[] mSetForegroundSignature = new Class[] {
	    boolean.class};
	private static final Class<?>[] mStartForegroundSignature = new Class[] {
	    int.class, Notification.class};
	private static final Class<?>[] mStopForegroundSignature = new Class[] {
	    boolean.class};

	private NotificationManager mNM;
	private Method mSetForeground;
	private Method mStartForeground;
	private Method mStopForeground;
	private Object[] mSetForegroundArgs = new Object[1];
	private Object[] mStartForegroundArgs = new Object[2];
	private Object[] mStopForegroundArgs = new Object[1];

	void invokeMethod(Method method, Object[] args) 
	{
	    try 
	    {
	        method.invoke(this, args);
	    } 
	    catch (InvocationTargetException e) 
	    {
	        // Should not happen.
	        Log.w("Perf Reader Service", "Unable to invoke method", e);
	    } catch (IllegalAccessException e) 
	    {
	        // Should not happen.
	        Log.w("Perf Reader Service", "Unable to invoke method", e);
	    }
	}


	@Override
	public IBinder onBind(Intent arg0) 
	{
		return null;
	}
	
	@Override
	public int onStartCommand(Intent intent, int flags, int startId)
	{					
		ConnectivityManager connManager = (ConnectivityManager) getSystemService(CONNECTIVITY_SERVICE);
	    NetworkInfo mWifi = connManager.getNetworkInfo(ConnectivityManager.TYPE_WIFI);

	    period = intent.getIntExtra("period", 25000);
		event0 = intent.getIntExtra("pmuEvent0", 0x06);
		event1 = intent.getIntExtra("pmuEvent1", 0x07);
		event2 = intent.getIntExtra("pmuEvent2", 0x0c);
		event3 = intent.getIntExtra("pmuEvent3", 0x0d);
		event4 = intent.getIntExtra("pmuEvent4", 0x0f);
		event5 = intent.getIntExtra("pmuEvent5", 0x12);
		
		ip = intent.getStringExtra("ip");
		if (ip == null)
		{
			ip = "192.168.11.23";
		}
		
		port = intent.getStringExtra("port");
		if (port == null)
		{
			port  = "5000";
		}
		
		Log.d("PMU", String.format("IP: %s", ip)); 
		Log.d("PMU", String.format("Port: %s", port)); 
		
		Log.d("PMU", String.format("Period: %d", period));
		Log.d("PMU", String.format("Event0: %d", event0));
		Log.d("PMU", String.format("Event1: %d", event1));
		Log.d("PMU", String.format("Event2: %d", event2));
		Log.d("PMU", String.format("Event3: %d", event3));
		Log.d("PMU", String.format("Event4: %d", event4));
		Log.d("PMU", String.format("Event5: %d", event5));    
	    
		// Only perform sampling if WiFi is connected..
	    if (mWifi.isConnected())
	    {
	        StartSampling();
	    }
	    else
	    {
	    	Toast.makeText(this, "Error: Wifi Not Connected", Toast.LENGTH_LONG).show();    		    	    						
	    }				
		
		return START_STICKY;
	}
	
	private void StartSampling()
	{
		lib = new NativeLib();
		int err = lib.initPmuGathering(period, event0, event1, event2, event3, event4, event5, ip, port);
		if (err != 0)
		{
			Toast.makeText(this, "ERROR: Unable to initialize PMU reader", Toast.LENGTH_LONG).show();
		}
				
		lib.startSampleGathering();		
		Toast.makeText(this, "PMU Reader Sampling Started", Toast.LENGTH_LONG).show();
	}
	
	@Override
	public void onCreate() 
	{
	    mNM = (NotificationManager)getSystemService(NOTIFICATION_SERVICE);
	    try 
	    {
	        mStartForeground = getClass().getMethod("startForeground",
	                mStartForegroundSignature);
	        mStopForeground = getClass().getMethod("stopForeground",
	                mStopForegroundSignature);
	        return;
	    } catch (NoSuchMethodException e) 
	    {
	        // Running on an older platform.
	        mStartForeground = mStopForeground = null;
	    }
	    
	    try 
	    {
	        mSetForeground = getClass().getMethod("setForeground",
	                mSetForegroundSignature);
	    } catch (NoSuchMethodException e) 
	    {
	        throw new IllegalStateException(
	                "OS doesn't have Service.startForeground OR Service.setForeground!");
	    }
	}

	
	@Override
	public void onDestroy()
	{
		super.onDestroy();
		lib.stopSampleGathering();
		Toast.makeText(this, "PMU Reader Service Stopped", Toast.LENGTH_LONG).show();
		
		// Make sure our notification is gone.
	    stopForegroundCompat(R.string.foreground_service_started);
	}
	
	/**
	 * This is a wrapper around the new startForeground method, using the older
	 * APIs if it is not available.
	 */
	void startForegroundCompat(int id, Notification notification) 
	{
	    // If we have the new startForeground API, then use it.
	    if (mStartForeground != null) {
	        mStartForegroundArgs[0] = Integer.valueOf(id);
	        mStartForegroundArgs[1] = notification;
	        invokeMethod(mStartForeground, mStartForegroundArgs);
	        return;
	    }

	    // Fall back on the old API.
	    mSetForegroundArgs[0] = Boolean.TRUE;
	    invokeMethod(mSetForeground, mSetForegroundArgs);
	    mNM.notify(id, notification);
	}

	/**
	 * This is a wrapper around the new stopForeground method, using the older
	 * APIs if it is not available.
	 */
	void stopForegroundCompat(int id) 
	{
	    // If we have the new stopForeground API, then use it.
	    if (mStopForeground != null) {
	        mStopForegroundArgs[0] = Boolean.TRUE;
	        invokeMethod(mStopForeground, mStopForegroundArgs);
	        return;
	    }

	    // Fall back on the old API.  Note to cancel BEFORE changing the
	    // foreground state, since we could be killed at that point.
	    mNM.cancel(id);
	    mSetForegroundArgs[0] = Boolean.FALSE;
	    invokeMethod(mSetForeground, mSetForegroundArgs);
	}
}
