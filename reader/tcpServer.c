/*
 *  TCP Server Program.  Reads information from the socket and writes it to a file.
 */

#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>
#include <pthread.h>

volatile int running = 1;
char* fileName = "outputSamples.txt";
int listenfd = 0, connfd = 0;
int port = 5000;

void serverReadThread();

void error(const char* msg)
{
    perror(msg);
    exit(1);
}

void *serverThread(void* ptr)
{
	serverReadThread();
}

void serverReadThread()
{
    int n = 0, itemsWritten = 0;
    //int listenfd = 0, connfd = 0;
    struct sockaddr_in serv_addr; 
    char recvBuff[1024*2];
    FILE *file;

    listenfd = socket(AF_INET, SOCK_STREAM, 0);

    if (listenfd < 0)
    {
        error("Error opening socket");
    }

    memset(&serv_addr, '0', sizeof(serv_addr));
    memset(recvBuff, '0', sizeof(recvBuff)); 

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
    serv_addr.sin_port = htons(port); 

    bind(listenfd, (struct sockaddr*)&serv_addr, sizeof(serv_addr));

 
    listen(listenfd, 10); 

    file = fopen(fileName, "w");

    printf("Socket and data file opened.  Data file is %s\n", fileName);
    while(running)
    {
        connfd = accept(listenfd, (struct sockaddr*)NULL, NULL); 

	while (running && (n = read(connfd, recvBuff, sizeof(recvBuff)-1)) > 0)
	{
		recvBuff[n] = 0;
		itemsWritten = fwrite(recvBuff, 1, n, file);
		
		//printf("RECEIVED DATA Size is %d\tItems Written: %d\n", n, itemsWritten);

		itemsWritten = 0;
	} 

	fflush(file);
        close(connfd);
        sleep(1);
    }

    fclose(file);
}


int main(int argc, char *argv[])
{
    pthread_t thread;
    char c = '\0';
    // See if there exists command line arg
    printf("\nRunning TCP Server Program.  Enter any key and press Enter to exit.\n\n");
    
    if (argc >= 2)
    {
        fileName = argv[1];
    }   
	
	if (argc >= 3)
	{
		port = atoi(argv[2]);
	}   

    pthread_create(&thread, NULL, serverThread, NULL);
    c = getchar();
   
    printf("Closing application...\n\n");
    running = 0;
    shutdown(listenfd, 2);
    shutdown(connfd, 2);
    pthread_join(thread, NULL);
}
