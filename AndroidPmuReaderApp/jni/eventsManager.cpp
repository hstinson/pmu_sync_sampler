#include <stdio.h>
#include <pthread.h>
#include <unistd.h>
#include <fcntl.h>
#include <android/log.h>
#include "stdint.h"

#include <sstream>
#include <string>
#include <unordered_map>
#include "TextReader.h"
#include "PacketSender/sender.h"

#define LOG_TAG "Perf_Out"
#define LOGI(x...) __android_log_print(ANDROID_LOG_DEBUG, LOG_TAG,x)
#define TRUE 1
#define FALSE 0

using namespace std;

int started = FALSE;
pthread_t readSamplesThreadId;

string ip;
string port;

TextReader *reader = new TextReader();
Sender *sender = new Sender();

int writeToSysFsFile(string filePath, string value)
{
	FILE* f = fopen(filePath.c_str(), "w");
	if (f == NULL)
	{
		LOGI("Unable to open path %s for writing\n", filePath.c_str());
		return 1;
	}

	fprintf(f, value.c_str());
	fclose(f);
	return 0;
}

string to_string (int num)
{
	ostringstream convert; // stream used for the conversion
	convert << num; // insert the textual representation of ‘Number’ in the characters    in the stream
	return convert.str();
}

void* readSamplesThread(void *arg)
{
	// Have sender code perform its task
	sender->setDebug(true);
	if (!sender->performSending(ip, port))
	{
		LOGI("Unable to send PMU samples.");
	}

	LOGI("\nExiting thread\n");
	return 0;
}

extern "C" int setupManager(int samplePeriodArg,
		unsigned int event0,
		unsigned int event1,
		unsigned int event2,
		unsigned int event3,
		unsigned int event4,
		unsigned int event5,
		const char * ipAddress,
		const char * portNum)
{
	int rv = 0;

	// Configure pmu driver by writing to character device driver
	rv |= writeToSysFsFile("/sys/sync_pmu/period", to_string(samplePeriodArg));
	rv |= writeToSysFsFile("/sys/sync_pmu/0", to_string(event0));
	rv |= writeToSysFsFile("/sys/sync_pmu/1", to_string(event1));
	rv |= writeToSysFsFile("/sys/sync_pmu/2", to_string(event2));
	rv |= writeToSysFsFile("/sys/sync_pmu/3", to_string(event3));
	rv |= writeToSysFsFile("/sys/sync_pmu/4", to_string(event4));
	rv |= writeToSysFsFile("/sys/sync_pmu/5", to_string(event5));

	ip = ipAddress;
	port = portNum;

	LOGI("Event0=%d,Event1=%d,Event2=%d,Event3=%d,Event4=%d,Event5=%d\n", event0,event1,event2,event3,event4,event5);

	return rv;
}

extern "C" void startManager(void)
{
	// Create thread...
	// Start up sampler module
	writeToSysFsFile("/sys/sync_pmu/status", "1");

	started = TRUE;
	int err = pthread_create(&readSamplesThreadId, NULL, &readSamplesThread, NULL);
	if (err != 0)
		LOGI("\nCan't create thread :[%s]", strerror(err));
}

extern "C" void stopManager(void)
{
	LOGI("\nSTOP CALLED");

	// Stop the sampling module
	writeToSysFsFile("/sys/sync_pmu/status", "0");

	// stop thread
	started = FALSE;
	pthread_join(readSamplesThreadId, NULL);
	LOGI("\nSTOP CALLED");
	fflush(stdout);
}
