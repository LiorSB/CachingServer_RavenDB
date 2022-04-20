# CachingServer_RavenDB

CachingServer - Is the main application, the caching server itself.<br/>
CachingServer_Tester - Is a client tester used to verify asynchronous work done by server.
<br/><br/>
To provide a TCP connection I used the class TcpListener and entered 127.0.0.1 which is the local host and the specified port (10011).<br/>
The application can handle concurrent connections with the help of Tasks which can execute a work asynchronously.<br/>
On all commands I took the assumption that they end with “\r\n” as specified in the question except for entering they key’s value that continues immediately after the specified size_in_bytes have been met.<br/>
Because the set value doesn’t include “\r\n” to the end of it I’ve changed “OK\r\n” to “\r\nOK\r\n” (see comments in lines 18-20 in CachingServer/Program.cs).<br/>
To store the data, I’ve used a dictionary and a queue.<br/>
The dictionary holds the key and value, in case a set command is entered with the same key then the last previous value will be overridden.<br/>
The queue will hold keys.<br/>
If the application has surpassed 128 MB in values, then keys will be dequeued from the queue which will be used to remove their value from the dictionary until there is enough room for the next value to enter.<br/>
The tester will run 100 clients each calling 100 set commands asynchronously.<br/>
<br/><br/>
https://user-images.githubusercontent.com/92099051/164186434-dbb6742a-690e-4b4a-bf57-95de3de3ac85.mp4
