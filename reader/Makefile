CFLAGS=-O2 -g -Wall -I../module/
CXXFLAGS=$(CFLAGS) -std=gnu++0x #-std=c++0x 

TARGETS = reader splitter server

all: $(TARGETS)

reader: reader.o packet.o text_reader.o
	$(CXX) $(LDFLAGS) -o $@ $^

splitter: reader.o packet.o splitter_reader.o
	$(CXX) $(LDFLAGS) -o $@ $^

server: tcpServer.o
	$(CXX) $(LDFLAGS) -o $@ $^ -pthread

packet.o: ../sender/packet.cpp
	$(CXX) $(CXXFLAGS) -c -o $@ $^

clean:
	rm -f *.o $(TARGETS)
