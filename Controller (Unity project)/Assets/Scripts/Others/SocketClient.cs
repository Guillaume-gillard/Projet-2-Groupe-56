using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class SocketClient
{
    private const int port = 51399;
    private const int recvLength = 1024;

    private Thread thread;
    private Socket socket;
    public Action<string, Content> onMessageReceive;
    public bool receiving { get; private set; }

    public struct Content
    {
        public static Content disconnectCode { get; } = new Content("<Disconnected>");

        public enum ContentType
        {
            String,
            Bytes
        }
        public readonly ContentType contentType;
        public readonly byte[] byteContent;
        public readonly string stringContent;

        public Content(string content)
        {
            byteContent = new byte[0];
            stringContent = content;
            contentType = ContentType.String;
        }

        public Content(byte[] content)
        {
            byteContent = content;
            stringContent = "";
            contentType = ContentType.Bytes;
        }
    }

    public void StartConnection(string ip)
    {
        // Connect to the server
        IPAddress address = IPAddress.Parse(ip);
        socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(new IPEndPoint(address, port));
    }

    public void Send(string header, string data)
    {
        // Encode data and send it to the client
        // header must be a 3 characters string
        Send(header, "s", Encoding.UTF8.GetBytes(data));
    }

    public void Send(string header, byte[] data)
    {
        // Encode data and send it to the client
        // header must be a 3 characters string
        Send(header, "b", data);
    }

    private void Send(string header, string contentType, byte[] data)
    {
        if (socket == null)
        {
            Debug.LogWarning("Connection wasn't started !");
            return;
        }

        try
        {
            byte[] bytes = new byte[8 + data.Length];
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(contentType + header), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, bytes, 4, 4);
            Buffer.BlockCopy(data, 0, bytes, 8, data.Length);
            socket.Send(bytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    public void StartReceive(Action<string, Content> onReceive)
    {
        // Start receiving messages in a thread
        if (socket == null)
        {
            Debug.LogWarning("Connection wasn't started !");
            return;
        }

        onMessageReceive = onReceive;
        receiving = true;
        thread = new Thread(Receive);
        thread.IsBackground = true;
        thread.Start();
    }

    public void StopReceive()
    {
        // Stop receiving messages
        receiving = false;
    }

    private void Receive()
    {
        // Wait for messages from the client and return them in the callback
        List<byte> remaining = new List<byte>();
        //Debug.Log("Start receiving");
        //System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            while (receiving)
            {
                //Debug.Log("New message");
                List<byte> message = remaining;
                string header = "";
                char contentType = ' ';
                int length = -1;

                // Receive parts of the message until it is complete
                while (length == -1 || message.Count < length)
                {
                    // Read header
                    if (length == -1 && message.Count > 7)
                    {
                        //watch = System.Diagnostics.Stopwatch.StartNew();
                        contentType = Convert.ToChar(message[0]);
                        header = Encoding.UTF8.GetString(message.GetRange(1, 3).ToArray());
                        length = BitConverter.ToInt32(message.GetRange(4, 4).ToArray(), 0);
                        if (length < 0) Debug.LogWarning(string.Join(", ", message.GetRange(0, 8)));
                        List<byte> start = message.GetRange(8, message.Count - 8);
                        message = new List<byte>(length + recvLength);
                        message.AddRange(start);
                        //Debug.Log($"Header read. Length: {length} - Header: {header}");
                        if (message.Count >= length) break;
                    }

                    //Debug.Log("Reading bytes");
                    // Get next part of buffer
                    byte[] bytes = new byte[recvLength];
                    int numByte = socket.Receive(bytes);
                    if (numByte == 0) receiving = false;
                    else
                    {
                        if (numByte == recvLength) message.AddRange(bytes);
                        else
                        {
                            byte[] realMessage = new byte[numByte];
                            Array.Copy(bytes, realMessage, numByte);
                            message.AddRange(realMessage);
                        }
                    }
                }

                // Call callback
                remaining = message.GetRange(length, message.Count - length);
                message = message.GetRange(0, length);
                if (receiving)
                {
                    if (contentType == 'b')
                    {
                        onMessageReceive.Invoke(header, new Content(message.ToArray()));
                    }
                    else if (contentType == 's')
                    {
                        onMessageReceive.Invoke(header, new Content(Encoding.UTF8.GetString(message.ToArray())));
                    }
                    //Debug.Log(watch.ElapsedMilliseconds);
                }
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning(e);
        }
        onMessageReceive.Invoke("", Content.disconnectCode);
    }

    public void GetIPFromBroadcast(Action<string> onReceive)
    {
        // Wait for a broadcast in a thread
        thread = new Thread(new ParameterizedThreadStart(delegate { WaitReceiveBroadcast(onReceive); }));
        thread.IsBackground = true;
        thread.Start();
    }

    private void WaitReceiveBroadcast(Action<string> onReceive)
    {
        UdpClient listener = new UdpClient(port);
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, port);
        listener.Receive(ref endpoint);
        listener.Close();
        onReceive.Invoke(endpoint.Address.ToString());
    }

    public void StopConnection()
    {
        // Disconnect from the server
        receiving = false;
        if (thread != null) thread.Abort();
        if (socket != null) socket.Close();
    }
}