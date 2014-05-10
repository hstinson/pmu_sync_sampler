
#include "sender.h"
#include <cstdio>
#include <netdb.h>
#include <errno.h>
#include <string.h>

#include <android/log.h>

#define LOG_TAG "Socket Sender"
#define LOGI(x...) __android_log_print(ANDROID_LOG_DEBUG, LOG_TAG,x)

using namespace std;

Sender::Sender()
{
	shutdown = false;
}

void Sender::stopSending()
{
	shutdown = true;
}

bool Sender::performSending(string& ipAddress, string& port)
{
	FILE *src = grab_device();
	FILE *miss = missed_buffer();
	int i = 0;

	if (debug)
	{
		packet_set_debug();
		network_set_debug();
	}

	LOGI("Making network connection with IP %s\tPort: %s\n", ipAddress.c_str(), port.c_str());
	if (make_connection(ipAddress.c_str(), port.c_str()))
	{
		return false;
	}

	LOGI("Network Connected.\n");

	if (send_header())
	{
		LOGI("Error sending header.\n");
		close_connection();
		return false;
	}

	LOGI("Starting sampling...\n");
	if (src && miss)
	{
		/* Clear things out so we can get a clear missed count */
		initial_missed = 0;
		for (i = 0; i < 2; ++i)
		{
			sample_device(src, miss, 1);
		}

		initial_missed = missed_count;
		outbytes = 0;
		sample_device(src, miss, 0);
	}
	LOGI("Sampling finished!\n");
	close_connection();

	if (src)
		fclose(src);
	if (miss)
		fclose(miss);

	return true;
}

//int main(int argc, const char** argv) {
//	const char *arg = NULL;
//	FILE *src = grab_device();
//	FILE *miss = missed_buffer();
//	int i = 0;
//
//	const char *program = *argv;
//	--argc; ++argv;
//
//	kbytes = 0;
//	debug = 0;
//	while (argc) {
//		if (!strcmp("-d", *argv)) {
//			debug = 1;
//			--argc; ++argv;
//			packet_set_debug();
//			network_set_debug();
//			continue;
//		}
//
//		if (!strcmp("-k", *argv)) {
//			--argc; ++argv;
//			if (!argc) {
//				fprintf(stderr, "-k requires an argument.\n");
//				return -1;
//			}
//			arg = *argv;
//			if (!*arg) {
//				fprintf(stderr, "-k requires a numeric argument.\n");
//				return -1;
//			}
//			for (; *arg && isdigit(*arg); ++arg);
//			if (*arg) {
//				fprintf(stderr, "-k requires a numeric argument.\n");
//				return -1;
//			}
//			kbytes = atoi(*argv);
//			if (!kbytes) {
//				fprintf(stderr, "-k = 0 means we do nothing.  exiting.\n");
//				return 0;
//			}
//			--argc; ++argv;
//			continue;
//		}
//
//		break;
//	}
//
//	if (argc < 2) {
//		fprintf(stderr, "Not enough arguments.  %s host port", program);
//		return -1;
//	}
//
//	if (make_connection(argv[0], argv[1]))
//		return -1;
//	fprintf(stderr, "Network Connected.\n");
//	argc -= 2;
//	argv += 2;
//
//	if (send_header()) {
//		fprintf(stderr, "Error sending header.\n");
//		close_connection();
//		return -1;
//	}
//
//	fprintf(stderr, "Starting sampling...\n");
//	if (src && miss) {
//		/* Clear things out so we can get a clear missed count */
//		initial_missed = 0;
//		for (i = 0; i < 2; ++i)
//			sample_device(src, miss, 1);
//		initial_missed = missed_count;
//		outbytes = 0;
//		sample_device(src, miss, 0);
//
//	}
//	fprintf(stderr, "Sampling finished!\n");
//	close_connection();
//
//	if (src)
//		fclose(src);
//	if (miss)
//		fclose(miss);
//
//	return 0;
//}

FILE *Sender::grab_device()
{
	FILE *f = fopen("/dev/pmu_samples", "rb");
	if (!f)
		LOGI("Error opening samples device:");
	return f;
}

FILE *Sender::missed_buffer()
{
	FILE *f = fopen("/sys/sync_pmu/missed", "r");
	if (!f)
		LOGI("Error opening missed buffers file:  %s\n", strerror(errno));
	return f;
}

int Sender::send_header()
{
	char period[64] = {0};
	char counter1[64] = {0};
	char counter2[64] = {0};
	char counter3[64] = {0};
	char counter4[64] = {0};
	char counter5[64] = {0};
	char counter6[64] = {0};

	char header[1024] = {0};

	if (debug) {
		LOGI("Getting values for header.\n");
		fflush(stderr);
	}

	if (grab_value(&period[0], 64, "period") ||
	    grab_value(&counter1[0], 64, "0") ||
	    grab_value(&counter2[0], 64, "1") ||
	    grab_value(&counter3[0], 64, "2") ||
	    grab_value(&counter4[0], 64, "3") ||
	    grab_value(&counter5[0], 64, "4") ||
	    grab_value(&counter6[0], 64, "5"))
		return -1;

	if (debug) {
		LOGI("Values retrieved.\n");
		fflush(stderr);
	}

	sprintf(&header[0],
	  "period:  %s"
	  "event1:  %s"
	  "event2:  %s"
	  "event3:  %s"
	  "event4:  %s"
	  "event5:  %s"
	  "event6:  %s"
	  "%c",
	  &period[0], &counter1[0], &counter2[0], &counter3[0],
	  &counter4[0], &counter5[0], &counter6[0], '\0');

	if (debug)
		LOGI("HEADER:\n%s\n", &header[0]);
	return network_packet(&header[0], strlen(&header[0]) + 1, NULL);
}

int Sender::grab_value(char *buffer, size_t n, const char *pmu_prop)
{
	char path[128] = {0};
	FILE *f = NULL;

	if (debug) {
		LOGI("Retrieving %s value from ", pmu_prop);
		fflush(stderr);
	}
	sprintf(&path[0], "/sys/sync_pmu/%s%c", pmu_prop, '\0');
	if (debug) {
		LOGI("%s:  ", &path[0]);
		fflush(stderr);
	}
	f = fopen(&path[0], "r");
	char* ignore = fgets(buffer, n-1, f);
	buffer[n-1] = '\0';
	if (debug) {
		LOGI("%s\n", &buffer[0]);
		fflush(stderr);
	}
	fclose(f);

	return 0;
}

int Sender::make_connection(const char *domain, const char *service)
{
	int network_status = network_init(domain, service);
	if (network_status)
		LOGI("network error: %s\n", gai_strerror(network_status));
	return network_status;
}

void Sender::sample_device(FILE *f, FILE *m, int test)
{
	struct buffer b;
	size_t read = 0;
	char *packet = NULL;
	char count[24] = {0}; // Never gonna fill up
	int i = 0;
	size_t sent = 0;

	while (!feof(f)) {
		size_t rc = fread(&b, sizeof(struct buffer), 1, f);
		char* rcc = fgets(&count[0], 23, m);
		missed_count = atoi(&count[0]) - initial_missed;
		if (!test && network_send(b, missed_count, &sent)) {
			LOGI("error:  Could not send batch from buffer:  %s\n", strerror(errno));
			break;
		}
		if (debug)
			debug_out(b);
		if (test)
			break;
		outbytes += sent;
		if (kbytes > 0 && outbytes / 1000 > kbytes) {
			LOGI("%zu kb requested, %zu bytes sent.\n", kbytes, outbytes);
			break;
		}
	}
}

void Sender::close_connection()
{
	network_finish();
}

/* JDD's code */
void Sender::debug_out(struct buffer& b) {
	for (size_t i=0; i<b.num_samples; i++) {
		struct sample& c = b.samples[i];
		ProcessInfo& pi = getProcessInfo(c.pid, packet_empty());
		LOGI("%lu,%u,%lu,%u,%u,%u,%u,%u,%u,%s,%s\n",
			c.pid, b.core, c.cycles,
			c.counters[0], c.counters[1], c.counters[2],
			c.counters[3], c.counters[4], c.counters[5],
			pi.cmdline.c_str(), pi.executable.c_str());
	}
}
