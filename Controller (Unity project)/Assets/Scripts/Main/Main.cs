using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

public class Main : MonoBehaviour
{
    public static Main instance;

    private SocketClient socket;
    public Transform settingsButton;
    public Transform mainPannel;
    public Text connectionText;
    public Transform exitPannel;
    public GameObject warning;
    public GameObject[] backgrounds;
    public bool demoBuild;

    private string ip = "";
    public bool connected;
    public bool newMessage;
    public SocketClient.Content message;
    public string header;

    public static readonly float kx = Screen.width / 1920f;
    public static readonly float ky = Screen.height / 1080f;
    public static readonly Vector3 kyScale = new Vector3(ky, ky, 1);

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Application.logMessageReceived += SendToFile;

        settingsButton.localScale = kyScale;
        settingsButton.position = new Vector2(Screen.width - 195 * ky, Screen.height - 75 * ky);
        mainPannel.localScale = kyScale;
        mainPannel.position = new Vector2(Screen.width / 2, Screen.height / 2);
        exitPannel.localScale = kyScale;
        exitPannel.position = new Vector2(Screen.width / 2, Screen.height / 2);
        warning.transform.localScale = kyScale;

        foreach(GameObject background in backgrounds)
        {
            float multiplier;
            if((float)Screen.width / Screen.height > 2) multiplier = 2 / ((float)Screen.height / 420);
            else multiplier = 4 / ((float)Screen.width / 420);
            background.GetComponent<Image>().pixelsPerUnitMultiplier = multiplier;
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(((int)(Screen.width / (420 / multiplier)) + 2) * (420 / multiplier), ((int)(Screen.height / (420 / multiplier)) + 2) * (420 / multiplier));
            background.transform.position = new Vector2(Screen.width / 2, Screen.height / 2);
        }

        if(!demoBuild) StartConnection();
    }

    private void Update()
    {
        if (ip != "" && !connected)
        {
            connected = true;
            Debug.Log("Connected !");
            StartCoroutine(Connect());
        }

        if (newMessage)
        {
            //Debug.Log("Message received");
            newMessage = false;
            if (!connected)
            {
                if (MetalMap.instance.gameObject.activeSelf)
                {
                    MetalMap.instance.Deactivate();
                }
                else if (ScanOptions.instance.gameObject.activeSelf)
                {
                    ScanOptions.instance.Deactivate();
                }
                connectionText.color = Color.red;
                connectionText.text = Translations.Translate("Connection : Waiting for robot ...");
                SendWarning(warning, Color.red, Translations.Translate("Robot disconnected !"));
                socket.StopConnection();
                StartConnection();
            }
            else if(header == "Map")
            {
                if (MetalMap.instance.gameObject.activeSelf)
                {
                    MetalMap.instance.UpdateMap(message.stringContent);
                }
            }
            else if (header == "Img")
            {
                if (MetalMap.instance.gameObject.activeSelf)
                {
                    MetalMap.instance.RenderCameraImage(message.byteContent);
                }
            }
            else if(header == "Res")
            {
                MetalMap.instance.SetCameraResolution(int.Parse(message.stringContent.Substring(0, message.stringContent.IndexOf(";"))), int.Parse(message.stringContent.Substring(message.stringContent.IndexOf(";")+1)));
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Settings.instance.gameObject.activeSelf)
            {
                Settings.instance.gameObject.SetActive(false);
            }
            else if (MetalMap.instance.gameObject.activeSelf)
            {
                if (MetalMap.instance.confirmingExit)
                {
                    MetalMap.instance.HideConfirmExit();
                }
                else if (FileBrowser.IsOpen)
                {
                    FileBrowser.HideDialog();
                }
                else
                {
                    MetalMap.instance.ShowConfirmExit();
                }
            }
            else if (ScanOptions.instance.gameObject.activeSelf)
            {
                ScanOptions.instance.Deactivate();
            }
            else
            {
                if (exitPannel.parent.gameObject.activeSelf)
                {
                    HideConfirmExit();
                }
                else
                {
                    ShowConfirmExit();
                }
            }
        }

        if(MetalMap.instance.gameObject.activeSelf && !MetalMap.instance.confirmingExit && Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            MetalMap.instance.SaveMap();
        }
    }

    private void OnApplicationQuit()
    {
        if (!demoBuild) socket.StopConnection();
    }


    // Connection //

    private void StartConnection() 
    {
        socket = new SocketClient();
        socket.GetIPFromBroadcast((string _ip) => ip = _ip);
    }

    private IEnumerator Connect()
    {
        yield return new WaitForSeconds(0.25f);
        socket.StartConnection(ip);
        socket.StartReceive(OnMessageReceive);
        connectionText.color = Color.green;
        connectionText.text = Translations.Translate("Connection : Robot connected !");
    }

    private void OnMessageReceive(string _header, SocketClient.Content content)
    {
        if(content.Equals(SocketClient.Content.disconnectCode))
        {
            ip = "";
            connected = false;
        }
        message = content;
        header = _header;
        newMessage = true;
    }

    public void SendInstruction(string message)
    {
        if(!demoBuild) socket.Send("Ins", message);
    }


    // Buttons //

    public void ControlledMode()
    {
        if (connected || demoBuild)
        {
            MetalMap.instance.Activate(1);
            SendInstruction($"controlled " + Settings.precision.ToString().Replace(',', '.'));
        }
        else
        {
            SendWarning(warning, Color.red, Translations.Translate("Robot not connected !"));
        }
    }

    public void ScanMode()
    {
        if (connected || demoBuild)
        {
            ScanOptions.instance.Activate();
        }
        else
        {
            SendWarning(warning, Color.red, Translations.Translate("Robot not connected !"));
        }
    }

    public void ShowConfirmExit()
    {
        exitPannel.parent.gameObject.SetActive(true);
    }

    public void HideConfirmExit()
    {
        exitPannel.parent.gameObject.SetActive(false);
    }

    public void QuitApplication()
    {
        if(connected) SendInstruction("shutdown");
        Application.Quit();
    }


    // Others //

    public void SendWarning(GameObject textObject, Color color, string message)
    {
        textObject.SetActive(true);
        textObject.transform.GetChild(0).gameObject.SetActive(true);
        textObject.GetComponent<Text>().text = message;
        textObject.transform.GetChild(0).gameObject.GetComponent<Text>().text = message;
        textObject.transform.GetChild(0).gameObject.GetComponent<Text>().color = color;
        textObject.GetComponent<Text>().color = Color.black;
        textObject.GetComponent<Text>().GetComponent<Fades>().StartFadeOut(0.6f, 1);
        textObject.transform.GetChild(0).gameObject.GetComponent<Fades>().StartFadeOut(0.6f, 1);
    }

    public void SendToFile(string logString, string stackTrace, LogType type)
    {
        // Send all logs to a file to be able to read them in the built version
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