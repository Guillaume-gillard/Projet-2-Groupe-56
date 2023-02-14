using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SocketClient
{
    public static int port = 51399;
    private Thread thread;
    private Socket socket;
    public Action<string> onMessageReceive;
    public bool receiving;

    public void StartConnection(string ip)
    {
        //Connect to the server
        IPAddress address = IPAddress.Parse(ip);
        socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(new IPEndPoint(address, port));
    }

    public void Send(string data)
    {
        //Encode data and send it to the client
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        socket.Send(bytes);
    }

    public void StartReceive(Action<string> onReceive)
    {
        //Start receiving messages in a thread
        onMessageReceive = onReceive;
        receiving = true;
        thread = new Thread(Receive);
        thread.IsBackground = true;
        thread.Start();
    }

    public void StopReceive()
    {
        //Stop receiving messages
        receiving = false;
    }

    private void Receive()
    {
        //Wait for messages from the client and return them in the callback
        while (receiving)
        {
            byte[] bytes = new byte[1024];
            int numByte = socket.Receive(bytes);
            if (numByte == 0) receiving = false;
            if(receiving) onMessageReceive.Invoke(Encoding.ASCII.GetString(bytes, 0, numByte));
        }
    }

    public void GetIPFromBroadcast(Action<string> onReceive)
    {
        //Wait for a broadcast in a thread
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
        //Disconnect from the server
        receiving = false;
        if(socket != null) socket.Close();
    }
}