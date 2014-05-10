#ifndef __EVENTSMANAGER_H__
#define __EVENTSMANAGER_H__

#ifdef __cplusplus
extern "C" {
#endif

int setupManager(int samplePeriodArg,
		unsigned int event0,
		unsigned int event1,
		unsigned int event2,
		unsigned int event3,
		unsigned int event4,
		unsigned int event5,
		const char * ipAddress,
		const char * portNum);

void startManager(void);

void stopManager(void);

#ifdef __cplusplus
}
#endif

#endif // __EVENTSMANAGER_H__
