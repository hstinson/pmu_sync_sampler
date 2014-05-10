/*
 * TextReader.cpp
 *
 *  Created on: Jul 29, 2013
 *      Author: hunter
 */

#include "TextReader.h"
#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <unistd.h>
#include <cassert>
#include <fstream>
#include <streambuf>

#include <android/log.h>

#define LOG_TAG "TextReader"
#define LOGI(x...) __android_log_print(ANDROID_LOG_DEBUG, LOG_TAG,x)

TextReader::TextReader()
{

}

void TextReader::readInto(unsigned long pid, const char* fn, string& into)
{
	char fnBuffer[256];
	snprintf(fnBuffer, 256, "/proc/%lu/%s", pid, fn);
	std::ifstream t(fnBuffer);
	into.assign((std::istreambuf_iterator<char>(t)),
                 std::istreambuf_iterator<char>());
}

void TextReader::readLinkPathInto(unsigned long pid, const char* fn, string& into)
{
	char fnBuffer[256], buf[1024];
	snprintf(fnBuffer, 256, "/proc/%lu/%s", pid, fn);
	ssize_t rc = readlink(fnBuffer, buf, sizeof(buf)-1);
	if (rc != -1)
	{
		buf[rc] = '\0';
		into = buf;
	}
	else
	{
		into = "";
	}
}


void TextReader::populate(ProcessInfoTextReader& pi)
{
	TextReader::readInto(pi.pid, "cmdline", pi.cmdline);
	TextReader::readLinkPathInto(pi.pid, "exe", pi.executable);

	if (pi.executable == "" && pi.cmdline == "")
		pi.mode = ProcessInfoTextReader::Kernel;
	else
		pi.mode = ProcessInfoTextReader::User;
}

ProcessInfoTextReader& TextReader::getProcessInfo(unsigned long pid)
{
	ProcessInfoTextReader& pi = procMap[pid];
	if (pi.mode == ProcessInfoTextReader::Unknown)
	{
		pi.pid = pid;
		TextReader::populate(pi);
	}
	return pi;
}

void TextReader::outputBuffer(buffer &buf)
{
	assert(buf.num_samples <= BUFFER_ENTRIES);
	for (size_t i=0; i<buf.num_samples; i++)
	{
		struct sample& c = buf.samples[i];
		ProcessInfoTextReader& pi = TextReader::getProcessInfo(c.pid);

		if (pi.mode == ProcessInfoTextReader::User &&
		    //pi.executable != "" &&
		    //pi.cmdline != "com.uah.stinson.perfcountertestapp" &&
		    pi.cmdline.compare("com.uah.stinson") <= 0) // &&
		    //pi.cmdline.compare("logcat") <= 0 &&
		    //pi.cmdline.compare("/sbin/adbd") <= 0)
		{
			LOGI("%lu,%u,%lu,%u,%u,%u,%u,%u,%u,%s,%s\n",
						c.pid, buf.core, c.cycles,
						c.counters[0], c.counters[1], c.counters[2],
						c.counters[3], c.counters[4], c.counters[5],
						pi.cmdline.c_str(), pi.executable.c_str());
		}
	}
}




