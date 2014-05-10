package com.uah.pmureader;

public class NativeLib
{
	static 
	{
		System.loadLibrary("perf_counter_lib");
	}
	
	public native int initPmuGathering(int samplePeriod, int event0, int event1, int event2, int event3, int event4, int event5, String ip, String port);
	
	public native void startSampleGathering();
	
	public native void stopSampleGathering();
}
