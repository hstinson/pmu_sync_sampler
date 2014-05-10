#include <stdio.h>
#include "eventsManager.h"
#include "com_uah_pmureader_NativeLib.h"

extern "C"
{
	JNIEXPORT jint JNICALL Java_com_uah_pmureader_NativeLib_initPmuGathering
	  (JNIEnv * env, jobject obj, jint samplePeriodArg, jint event0, jint event1, jint event2, jint event3, jint event4, jint event5, jstring ip, jstring port)
	{
		const char* ipStr = env->GetStringUTFChars(ip,0);
		const char* portStr = env->GetStringUTFChars(port,0);

		return setupManager(samplePeriodArg, event0, event1, event2, event3, event4, event5, ipStr, portStr);

		env->ReleaseStringUTFChars(ip,ipStr);
		env->ReleaseStringUTFChars(port,portStr);
	}


	JNIEXPORT void JNICALL Java_com_uah_pmureader_NativeLib_startSampleGathering
	  (JNIEnv *env, jobject obj)
	{
		startManager();
	}


	JNIEXPORT void JNICALL JNICALL Java_com_uah_pmureader_NativeLib_stopSampleGathering
	  (JNIEnv *env, jobject obj)
	{
		stopManager();
	}

}
