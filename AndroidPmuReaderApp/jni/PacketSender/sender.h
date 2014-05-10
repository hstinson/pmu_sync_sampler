/*
 * sender.h
 *
 *  Created on: Aug 8, 2013
 *      Author: hunter
 */

#ifndef SENDER_H_
#define SENDER_H_

#include <cstdio>
#include <netdb.h>
#include <errno.h>
#include <string.h>

#include "network.h"
#include "packet.h"
#include "../SampleBuffer.h"
#include "process_info.h"

static int debug;

using namespace std;

class Sender
{
private:
	int initial_missed;
	int missed_count;
	bool shutdown;

	size_t kbytes;
	size_t outbytes;

	FILE *grab_device();
	FILE *missed_buffer();

	int make_connection(const char *domain, const char *service);
	void sample_device(FILE *device, FILE *missed, int test);
	int send_header();
	void close_connection();
	void debug_out(struct buffer &b);

	int grab_value(char *buffer, size_t n, const char *pmu_prop);

public:
	bool performSending(string& ipAddress, string& port);
	void stopSending();
	Sender();

	int getDebug() const
	{
		return debug;
	}

	void setDebug(int debugArg)
	{
		debug = debugArg;
	}
};


#endif /* SENDER_H_ */
