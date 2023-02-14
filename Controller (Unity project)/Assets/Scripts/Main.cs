using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    private SocketClient socket;
    public GameObject input;
    public GameObject text;

    private string ip = "";
    public bool newMessage;
    public string message;

    public static float kx = Screen.width / 1920f;
    public static float ky = Screen.height / 1080;

    private void Start()
    {
        Application.logMessageReceived += sendToFile;
        input.transform.localScale = new Vector3(kx, kx, 1);
        input.transform.position = new Vector3(Screen.width / 2, Screen.height / 4, 0);
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width - 50, Screen.height / 2);
        text.transform.position = new Vector3(Screen.width / 2, 3 * Screen.height / 4, 0);
        StartConnection();
    }

    private void Update()
    {
        if (newMessage)
        {
            newMessage = false;
            text.GetComponent<Text>().text = message;
        }
        if (ip != "")
        {
            StartCoroutine(Connect(ip));
            ip = "";
        }
    }

    private void OnApplicationQuit()
    {
        socket.StopConnection();
    }

    private void StartConnection() 
    {
        socket = new SocketClient();
        text.GetComponent<Text>().text = "Waiting for broadcast...";
        socket.GetIPFromBroadcast((string _ip) => ip = _ip);
    }

    private IEnumerator Connect(string ip)
    {
        text.GetComponent<Text>().text = "Connecting...";
        yield return new WaitForSeconds(0.25f);
        socket.StartConnection(ip);
        socket.StartReceive(OnMessageReceived);
        text.GetComponent<Text>().text = "Connected!";
    }

    private void OnMessageReceived(string _message)
    {
        message = _message;
        newMessage = true;
    }

    public void SendMessage()
    {
        string message = input.GetComponent<InputField>().text;
        if (message == "exit") Application.Quit();
        else socket.Send(message);
        input.GetComponent<InputField>().text = "";
    }

    public void sendToFile(string logString, string stackTrace, LogType type)
    {
        //Send all logs to a file to be able to read them in the built version
        string path = Application.persistentDataPath + "/Logs.txt";
        string content;
        string title;
        if (File.Exists(path))
        {
            StreamReader reader = new StreamReader(path);
            content = reader.ReadToEnd();
            reader.Close();
            if (type == LogType.Exception || type == LogType.Error) title = "\n~e\n";
            else title = "\n~o\n";
            content += title + "[" + System.DateTime.Now + "] " + logString + " | " + stackTrace;
            if (content.Length > 2000000)
            {
                content = content.Substring(content.Length - 2000000);
                content = "[...] " + content;
            }
        }
        else
        {
            if (logString.StartsWith("{\"_sceneLoaded\"")) title = "\n~s\n";
            else title = "\n~o\n";
            content = title + "[" + System.DateTime.Now + "] " + logString + " | " + stackTrace;
        }
        TextWriter writer = new StreamWriter(path, false);
        writer.Write(content);
        writer.Close();
    }
}
