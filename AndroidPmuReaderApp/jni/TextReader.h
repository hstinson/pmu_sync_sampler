#ifndef __TEXTREADER_H_
#define __TEXTREADER_H_

#include "SampleBuffer.h"
#include <stdint.h>
#include <unistd.h>
#include <unordered_map>
#include <string>
#include <map>

using namespace std;

struct ProcessInfoTextReader
{
	uint32_t pid;
	enum
	{
		Unknown,
		Kernel,
		User
	} mode;
	string cmdline;
	string executable;

	ProcessInfoTextReader()
	{
		pid = 0;
		mode = Unknown;
		cmdline = "";
		executable = "";
	}
};

class TextReader
{
private:
	//std::unordered_map<unsigned long, ProcessInfoTextReader> procMap;
	std::map<int, ProcessInfoTextReader> procMap;
	pid_t appPid;

	void readInto(unsigned long pid, const char* fn, string& into);
	void readLinkPathInto(unsigned long pid, const char* fn, string& into);
	void populate(ProcessInfoTextReader& pi);
	ProcessInfoTextReader& getProcessInfo(unsigned long pid);

public:
	void outputBuffer(struct buffer& b);
	TextReader();

};


#endif // __TEXTREADER_H_
