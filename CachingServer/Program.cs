using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CachingServer
{
    public class Program
    {
        // 128 MB is the maximum amount of cached data.
        private const int MAX_BYTES = 128000000;
        private const string SET_COMMAND = "set";
        private const string GET_COMMAND = "get";
        private const string MISSING_MESSAGE = "MISSING\r\n";
        private const string INVALID_MESSAGE = "Invalid Command!\r\n";
        // The 'OK' message start with \r\n to save the user from pressing enter at end of value.
        // In case the user should press enter - currentBytes in line 101 for set command should be increased by 2.
        // Then in get command in line 140 \r\n at end of message should be removed.
        private const string OK_MESSAGE = "\r\nOK\r\n";
        // Saves the amount of current bytes within the system.
        private static int _totalBytes = 0;
        private static Dictionary<string, string> _dataByKey = new();
        private static Queue<string> _keysQueue = new();
        private static object _synchronizeClients = new();

        public static void Main()
        {
            TcpListener server = null;

            try
            {
                Int32 port = 10011;
                IPAddress localAddress = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddress, port);
                server.Start();

                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");

                    TcpClient client = server.AcceptTcpClient();
                    Task.Run(() => ProcessClient(client));
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine($"SocketException: {se}");
            }
            finally
            {
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private static void ProcessClient(TcpClient client)
        {
            Byte[] buffer = new Byte[1];
            string clientMessage = string.Empty;
            NetworkStream stream = client.GetStream();

            Console.WriteLine("Connected!");

            byte[] responseMessage = System.Text.Encoding.ASCII.GetBytes("Connected to 127.0.0.1\r\n");
            stream.Write(responseMessage, 0, responseMessage.Length);

            int numberOfBytes;

            while ((numberOfBytes = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string data = System.Text.Encoding.ASCII.GetString(buffer, 0, numberOfBytes);
                clientMessage += data;

                // Continue receiving data from client untill \r\n has been entered.
                if (clientMessage.Substring(Math.Max(0, clientMessage.Length - 2)) != "\r\n")
                {
                    continue;
                }

                Console.WriteLine($"Received: {clientMessage}");

                List<string> splitMessage = (clientMessage.Split(null)).ToList();

                string serverMessage;

                if (splitMessage[0] == SET_COMMAND)
                {
                    serverMessage = PerformSetCommand(buffer, stream, splitMessage[1], Int32.Parse(splitMessage[2]));
                }
                else if (splitMessage[0] == GET_COMMAND)
                {
                    serverMessage = PerformGetCommand(splitMessage[1]);
                }
                else
                {
                    serverMessage = INVALID_MESSAGE;
                }

                responseMessage = System.Text.Encoding.ASCII.GetBytes(serverMessage);
                stream.Write(responseMessage, 0, responseMessage.Length);

                // In order to verify async, remove comment of finish.
                //Console.WriteLine($"Finished: {clientMessage}");

                clientMessage = string.Empty;
            }

            client.Close();
        }

        private static string PerformSetCommand(byte[] buffer, NetworkStream stream, string key, int currentBytes)
        {
            int numberOfBytes;
            string dataValue = string.Empty;

            for (int j = 0; j < currentBytes; j++)
            {
                numberOfBytes = stream.Read(buffer, 0, buffer.Length);
                dataValue += System.Text.Encoding.ASCII.GetString(buffer, 0, numberOfBytes);
            }

            lock (_synchronizeClients)
            {
               if (_dataByKey.ContainsKey(key))
                {
                    _totalBytes -= _dataByKey[key].Length;
                    _dataByKey.Remove(key);
                }

                // Delete values from cache until there is enough space for the new value.
                while (_totalBytes + currentBytes > MAX_BYTES)
                {
                    string keyToDelete = _keysQueue.Dequeue();

                    // Key may not be present, if we replaced it in line 134 due to recieving a key that already exists.
                    if (_dataByKey.ContainsKey(keyToDelete))
                    {
                        _totalBytes -= _dataByKey[keyToDelete].Length;
                        _dataByKey.Remove(keyToDelete);
                        //Console.WriteLine($"Deleted Key: {keyToDelete}");
                    }
                }
                
                 _totalBytes += currentBytes;

                _dataByKey.Add(key, dataValue);
                _keysQueue.Enqueue(key);
            }

            return OK_MESSAGE;
        }

        private static string PerformGetCommand(string key)
        {
            string serverMessage;

            if (_dataByKey.ContainsKey(key))
            {
                serverMessage = $"OK {_dataByKey[key].Length}\r\n{_dataByKey[key]}\r\n";
            }
            else
            {
                serverMessage = MISSING_MESSAGE;
            }

            return serverMessage;
        }
    }
}
