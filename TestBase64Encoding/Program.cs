using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace TestBase64Encoding
{
    // Quick and dirty program for checking performance of raw byte streaming vs serialization to JSON data
    class Program
    {
        private static readonly int _iterations = 1000;
        private static readonly int _maxExpectedMessages = 800;
        private static readonly int _minExpectedMessageSizeBytes = 259;
        private static readonly int _maxExpectedMessageSizeBytes = 300;
        
        class MessagePayload
        {
            public List<byte[]> Messages { get; set; }
        }
        
        static void Main()
        {
            var serializerTimes = new List<double>();
            var rawTimes = new List<double>();
            var deserializerTimes = new List<double>();

            Console.WriteLine("Starting iterations...");

            for (int i = 0; i < _iterations; i++)
            {
                var random = new Random();
                var byteArrays = new List<byte[]>();

                for (int j = 0; j < _maxExpectedMessages; j++)
                {
                    var bytes = new byte[random.Next(_minExpectedMessageSizeBytes, _maxExpectedMessageSizeBytes)];
                    random.NextBytes(bytes);

                    byteArrays.Add(bytes);
                }

                var serializerData = GetSerializePerformance(byteArrays);
                serializerTimes.Add(serializerData.Item1);
                
                rawTimes.Add(GetRawPerformance(byteArrays));
                
                deserializerTimes.Add(GetDeserializePerformance(serializerData.Item2));
            }
            
            Console.WriteLine($"Average timing for serializations: {serializerTimes.Average()}");
            Console.WriteLine($"Average timing for raw: {rawTimes.Average()}");
            Console.WriteLine($"Average timing for deserializations: {deserializerTimes.Average()}");
            Console.WriteLine("===================================================");
            Console.WriteLine($"Max timing for serializations: {serializerTimes.Max()}");
            Console.WriteLine($"Max timing for raw: {rawTimes.Max()}");
            Console.WriteLine($"Max timing for deserializations: {deserializerTimes.Max()}");
        }

        private static (double, string) GetSerializePerformance(List<byte[]> messages)
        {
            // Test performance of serializing to JSON
            var wrapperObject = new MessagePayload {Messages = messages};
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var jsonData = JsonSerializer.Serialize(wrapperObject);
            stopWatch.Stop();

            return (stopWatch.Elapsed.TotalMilliseconds, jsonData);
        }

        private static double GetDeserializePerformance(string json)
        {
            // Test performance of deserializing from JSON
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var messagesData = JsonSerializer.Deserialize<MessagePayload>(json);
            stopWatch.Stop();

            return stopWatch.Elapsed.TotalMilliseconds;
        }

        private static double GetRawPerformance(List<byte[]> messages)
        {
            // Test performance of working with the bytes directly
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            
            // We need 2 bytes for the length of the message + 2 bytes for the offset
            var totalHeaderLength = messages.Count * 8;
            var totalMessageLength = 0;

            for (int i = 0; i < messages.Count; i++)
            {
                totalMessageLength += messages[i].Length;
            }

            var combinedByteArray = new byte[totalMessageLength + totalHeaderLength];
            var arrayIndex = 0;

            for (int i = 0; i < messages.Count; i++)
            {
                var offsetBytes = BitConverter.GetBytes(i);
                offsetBytes.CopyTo(combinedByteArray, arrayIndex);

                arrayIndex += offsetBytes.Length;

                var messageCountBytes = BitConverter.GetBytes(messages[i].Length);
                messageCountBytes.CopyTo(combinedByteArray, arrayIndex);

                arrayIndex += messageCountBytes.Length;
                
                messages[i].CopyTo(combinedByteArray, arrayIndex);

                arrayIndex += messages[i].Length;
            }
            
            stopWatch.Stop();

            return stopWatch.Elapsed.TotalMilliseconds;
        }
    }
}