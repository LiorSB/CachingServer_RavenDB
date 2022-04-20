using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CachingClient_Tester
{
    // Testing application.
    public class Program
    {
        public static void Main(string[] args)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                int c = i;
                tasks.Add(Task.Run(() => OpenClient(c)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private static void OpenClient(int id)
        {
            try
            {
                TcpClient client = new("127.0.0.1", 10011);

                NetworkStream stream = client.GetStream();

                Byte[] data = new Byte[256];
                String responseData = String.Empty;
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine(responseData);

                for (int i = 0; i < 100; i++)
                {
                    string setMessage = $"set {id}email{i} 16\r\njobs@ravendb.net";

                    data = System.Text.Encoding.ASCII.GetBytes(setMessage);
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine(setMessage);

                    data = new Byte[256];
                    responseData = String.Empty;
                    bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    Console.WriteLine(responseData);
                }

                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }
    }
}
