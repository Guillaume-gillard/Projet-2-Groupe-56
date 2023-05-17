using System;
using System.Collections;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using SimpleFileBrowser;

public class MetalMap : MonoBehaviour
{
    public static MetalMap instance { get; private set; }

    public GameObject forwardCursor;
    public GameObject directionCursor;
    public GameObject joystick;
    public RectTransform mainMap;
    public RectTransform miniMap;
    public Transform buttons;
    public Transform exitPannel;
    public Texture2D defaultCameraTexture;
    public Camera renderCamera;
    public Text xAxis;
    public Text yAxis;
    public RectTransform robot;
    public Text fpsText;

    public Vector2Int cameraResolution { get; private set; }
    private const int mapResolution = 1000;
    public bool confirmingExit { get; private set; }
    private Texture2D mapTexture;
    private Texture2D cameraTexture;
    private RenderTexture renderTexture;
    private bool cameraMain;
    public int mode { get; private set; }

    public Vector2 originPos;
    public float scale;
    private long lastFrame = 0;
    public bool isSaving;
    private string oldData = "";

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        forwardCursor.transform.localScale = Main.kyScale;
        forwardCursor.GetComponent<Cursor>().startPosition = new Vector2(130 * Main.ky, 200 * Main.ky);
        forwardCursor.transform.position = new Vector2(130 * Main.ky, 200 * Main.ky);
        forwardCursor.GetComponent<Cursor>().startTouchArea = new Rect(0, 0, Screen.width * 0.4f, Screen.height * 0.6f);
        forwardCursor.GetComponent<Cursor>().maxMovement = (int)(95 * Main.ky);
        directionCursor.transform.localScale = Main.kyScale;
        directionCursor.GetComponent<Cursor>().startPosition = new Vector2(Screen.width - 200 * Main.ky, 200 * Main.ky);
        directionCursor.transform.position = new Vector2(Screen.width - 200 * Main.ky, 200 * Main.ky);
        directionCursor.GetComponent<Cursor>().startTouchArea = new Rect(Screen.width * 0.6f, 0, Screen.width * 0.4f, Screen.height * 0.6f);
        directionCursor.GetComponent<Cursor>().maxMovement = (int)(95 * Main.ky);
        joystick.transform.localScale = Main.kyScale;
        joystick.GetComponent<Cursor>().startPosition = new Vector2(200 * Main.ky, 200 * Main.ky);
        joystick.transform.position = new Vector2(200 * Main.ky, 200 * Main.ky);
        joystick.GetComponent<Cursor>().startTouchArea = new Rect(0, 0, Screen.width * 0.4f, Screen.height * 0.6f);
        joystick.GetComponent<Cursor>().maxMovement = (int)(110 * Main.ky);
        buttons.localScale = Main.kyScale;
        buttons.position = new Vector3(Screen.width - 165 * Main.ky, Screen.height - 165 * Main.ky);
        exitPannel.localScale = Main.kyScale;
        exitPannel.position = new Vector2(Screen.width / 2, Screen.height / 2);

        mainMap.localScale = Main.kyScale;
        mainMap.position = new Vector2(Screen.width / 2, Screen.height / 2);
        if((Screen.width - 1025 * Main.ky) / 2 < 400 * Main.ky)
        {
            float k = (Screen.width - 1025 * Main.ky) / 800;
            miniMap.localScale = new Vector3(k, k, 1);
            miniMap.position = new Vector3(200 * k, Screen.height - 27.5f * Main.ky);
        }
        else
        {
            miniMap.localScale = Main.kyScale;
            miniMap.position = new Vector3(202.5f * Main.ky, Screen.height - 27.5f * Main.ky);
        }
        cameraResolution = new Vector2Int(500, 500);

        renderTexture = new RenderTexture(mapResolution, mapResolution, 24);
        renderCamera.targetTexture = renderTexture;

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Image", ".png"));
        FileBrowser.SetDefaultFilter(".png");

        gameObject.SetActive(false);
    }

    public void Activate(int _mode)
    {
        gameObject.SetActive(true);
        cameraTexture = new Texture2D(defaultCameraTexture.width, defaultCameraTexture.height);
        cameraTexture.SetPixels(defaultCameraTexture.GetPixels());
        cameraTexture.Apply();
        mapTexture = new Texture2D(mapResolution, mapResolution);
        mapTexture.SetPixels(Enumerable.Repeat(Color.white, mapResolution * mapResolution).ToArray());
        mapTexture.Apply();
        if (cameraMain)
        {
            miniMap.GetChild(0).GetComponent<RawImage>().texture = mapTexture;
            mainMap.GetChild(0).GetComponent<RawImage>().texture = cameraTexture;
        }
        else
        {
            miniMap.GetChild(0).GetComponent<RawImage>().texture = cameraTexture;
            mainMap.GetChild(0).GetComponent<RawImage>().texture = mapTexture;
        }
        originPos = new Vector2(mapResolution / 2, mapResolution / 2);
        scale = mapResolution / 100f;
        SetScale(scale);
        SetRobotPosition(originPos, 0);
        MovementsPredict.instance.Restart();
        mode = _mode;
        UpdateControls();
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
        if(Main.instance.connected) Main.instance.SendInstruction("end");
        if (Main.instance.demoBuild)
        {
            Settings.instance.PrecisionChange(true);
        }
    }

    public void UpdateControls()
    {
        if(mode == 0)
        {
            joystick.SetActive(false);
            forwardCursor.SetActive(false);
            directionCursor.SetActive(false);
        }
        else if(mode == 1)
        {
            joystick.SetActive(Settings.controls == 0);
            forwardCursor.SetActive(Settings.controls == 1);
            directionCursor.SetActive(Settings.controls == 1);
        }
    }

    public void UpdateMap(string mapData = "")
    {
        if (mapData == "")
        {
            mapData = oldData;
            if (mapData == "") return;
        }
        oldData = mapData;

        string[] data = mapData.Split(';');
        Vector2 robotPos = new Vector2(float.Parse(data[0].Replace(',', '.'), CultureInfo.InvariantCulture), float.Parse(data[1].Replace(',', '.'), CultureInfo.InvariantCulture));
        float orientation = float.Parse(data[2].Replace(',', '.'), CultureInfo.InvariantCulture) * Mathf.Rad2Deg;
        Vector2Int originIndex = new Vector2Int(int.Parse(data[3].Replace(',', '.'), CultureInfo.InvariantCulture), int.Parse(data[4].Replace(',', '.'), CultureInfo.InvariantCulture));
        float[][] map = JsonConvert.DeserializeObject<float[][]>(data[5]);
        for(int i = 0; i < map.Length; i++)
        {
            for(int j = 0; j < map[0].Length; j++)
            {
                if(map[i][j] != -1)
                {
                    map[i][j] = map[i][j] / (1 - Settings.sensitivity);
                }
            }
        }

        // Scale
        float precision = mode == 1 ? Settings.precision : Settings.scanPrecision;
        float borderProportion = precision / (mapResolution / scale);
        int sizeX = (int)((map.Length * precision * (1 + 2 * borderProportion)) / 50);
        if (sizeX == 0) sizeX = 1;
        int sizeY = (int)((map[0].Length * precision * (1 + 2 * borderProportion)) / 50);
        if (sizeY == 0) sizeY = 1;
        float oldScale = scale;
        if (sizeX > sizeY) scale = mapResolution / (50f * (sizeX + 1));
        else scale = mapResolution / (50f * (sizeY + 1));
        if (scale > oldScale) scale = oldScale;
        SetScale(scale);

        // Origin position
        Vector2 relativePos = robotPos * scale + originPos;
        if (relativePos.x < mapResolution * borderProportion) originPos.x += mapResolution * (borderProportion / 2) * ((int)((mapResolution * borderProportion - relativePos.x) / (mapResolution * (borderProportion / 2))) + 1);
        else if (relativePos.x > mapResolution * (1 - borderProportion)) originPos.x -= mapResolution * (borderProportion / 2) * ((int)((relativePos.x - mapResolution * (1 - borderProportion)) / (mapResolution * (borderProportion / 2))) + 1);
        if (relativePos.y < mapResolution * borderProportion) originPos.y += mapResolution * (borderProportion / 2) * ((int)((mapResolution * borderProportion - relativePos.y) / (mapResolution * (borderProportion / 2))) + 1);
        else if (relativePos.y > mapResolution * (1 - borderProportion)) originPos.y -= mapResolution * (borderProportion / 2) * ((int)((relativePos.y - mapResolution * (1 - borderProportion)) / (mapResolution * (borderProportion / 2))) + 1);
        SetRobotPosition(robotPos * scale + originPos, orientation);
        MovementsPredict.instance.SetPosition(robotPos, orientation);

        //System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        // Colors
        Color32 color1 = Settings.color1;
        Color32 color2 = Settings.color2;
        mapTexture.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255), mapResolution * mapResolution).ToArray());
        int[,,] directions = new int[,,]
        {
            { { -1, 0 }, { 0, 1 } },
            { { 1, 0 }, { 0, 1 } },
            { { -1, 0 }, { 0, -1 } },
            { { 1, 0 }, { 0, -1 } },
        };
        int[,] positions = new int[,]
        {
            { 3, 2, 1, 0 },
            { 2, 3, 0, 1 },
            { 1, 0, 3, 2 },
            { 0, 1, 2, 3 },
        };
        for (int i = 0; i < map.Length; i++)
        {
            for (int j = 0; j < map[0].Length; j++)
            {
                if (map[i][j] != -1)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        Color32[] corners = new Color32[4];
                        int x1 = i + directions[k, 0, 0];
                        int x2 = i + directions[k, 1, 0];
                        int x3 = i + directions[k, 0, 0] + directions[k, 1, 0];
                        int y1 = j + directions[k, 0, 1];
                        int y2 = j + directions[k, 1, 1];
                        int y3 = j + directions[k, 0, 1] + directions[k, 1, 1];
                        bool corner1 = x1 >= 0 && x1 < map.Length && y1 >= 0 && y1 < map[0].Length && map[x1][y1] != -1;
                        bool corner2 = x2 >= 0 && x2 < map.Length && y2 >= 0 && y2 < map[0].Length && map[x2][y2] != -1;
                        bool corner3 = x3 >= 0 && x3 < map.Length && y3 >= 0 && y3 < map[0].Length && map[x3][y3] != -1;

                        corners[0] = Color32.Lerp(color1, color2, map[i][j]);
                        if (corner1) corners[1] = Color32.Lerp(Color32.Lerp(color1, color2, map[i][j]), Color32.Lerp(color1, color2, map[x1][y1]), 0.5f);
                        else corners[1] = corners[0];
                        if (corner2) corners[2] = Color32.Lerp(Color32.Lerp(color1, color2, map[i][j]), Color32.Lerp(color1, color2, map[x2][y2]), 0.5f);
                        else corners[2] = corners[0];
                        if (corner1 && corner2)
                        {
                            if (corner3) corners[3] = Color32.Lerp(corners[1], Color32.Lerp(Color32.Lerp(color1, color2, map[x2][y2]), Color32.Lerp(color1, color2, map[x3][y3]), 0.5f), 0.5f);
                            else corners[3] = Color32.Lerp(Color32.Lerp(color1, color2, map[x1][y1]), Color32.Lerp(color1, color2, map[x2][y2]), 0.5f);
                        }
                        else if (corner1 || corner2)
                        {
                            if (corner1)
                            {
                                if (corner3) corners[3] = Color32.Lerp(Color32.Lerp(color1, color2, map[i][j]), Color32.Lerp(color1, color2, map[x3][y3]), 0.5f);
                                else corners[3] = corners[1];
                            }
                            else
                            {
                                if (corner3) corners[3] = Color32.Lerp(Color32.Lerp(color1, color2, map[i][j]), Color32.Lerp(color1, color2, map[x3][y3]), 0.5f);
                                else corners[3] = corners[2];
                            }
                        }
                        else corners[3] = corners[0];

                        x1 = (int)(originPos.x + (i - originIndex.x) * precision * scale);
                        x2 = (int)(originPos.x + ((x3 + i) / 2f - originIndex.x) * precision * scale);
                        if (x1 > x2) { x3 = x1; x1 = x2; x2 = x3; }
                        y1 = (int)(originPos.y + (j - originIndex.y) * precision * scale);
                        y2 = (int)(originPos.y + ((y3 + j) / 2f - originIndex.y) * precision * scale);
                        if (y1 > y2) { y3 = y1; y1 = y2; y2 = y3; }

                        mapTexture.SetPixels32(x1, y1, x2 - x1, y2 - y1, Interpolate(corners[positions[k, 0]], corners[positions[k, 1]], corners[positions[k, 2]], corners[positions[k, 3]], x2 - x1, y2 - y1));
                    }
                }
            }
        }

        //Debug.Log(watch.ElapsedMilliseconds);

        mapTexture.Apply();
    }

    public void SwapMaps()
    {
        if (cameraMain)
        {
            mainMap.sizeDelta = new Vector2(mainMap.sizeDelta.x, mainMap.sizeDelta.x);
            miniMap.sizeDelta = new Vector2(miniMap.sizeDelta.x, miniMap.sizeDelta.x * cameraResolution.y / cameraResolution.x);
            mainMap.GetChild(0).GetComponent<RawImage>().texture = mapTexture;
            miniMap.GetChild(0).GetComponent<RawImage>().texture = cameraTexture;
            mainMap.GetChild(0).localScale = new Vector3(1, 1, 1);
            miniMap.GetChild(0).localScale = new Vector3(-1, -1, 1);
            robot.gameObject.SetActive(true);
            xAxis.transform.parent.gameObject.SetActive(true);
            cameraMain = false;
        }
        else
        {
            miniMap.sizeDelta = new Vector2(miniMap.sizeDelta.x, miniMap.sizeDelta.x);
            mainMap.sizeDelta = new Vector2(mainMap.sizeDelta.x, mainMap.sizeDelta.x * cameraResolution.y / cameraResolution.x);
            miniMap.GetChild(0).GetComponent<RawImage>().texture = mapTexture;
            mainMap.GetChild(0).GetComponent<RawImage>().texture = cameraTexture;
            miniMap.GetChild(0).localScale = new Vector3(1, 1, 1);
            mainMap.GetChild(0).localScale = new Vector3(-1, -1, 1);
            robot.gameObject.SetActive(false);
            xAxis.transform.parent.gameObject.SetActive(false);
            cameraMain = true;
        }
    }

    public void SetCameraResolution(int x, int y)
    {
        cameraResolution = new Vector2Int(x, y);
        if (cameraMain)
        {
            mainMap.sizeDelta = new Vector2(mainMap.sizeDelta.x, mainMap.sizeDelta.x * y / x);
        }
        else
        {
            miniMap.sizeDelta = new Vector2(miniMap.sizeDelta.x, miniMap.sizeDelta.x * y / x);
        }
    }

    public void RenderCameraImage(byte[] bytes)
    {
        if (fpsText.gameObject.activeSelf)
        {
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            LoadImage(bytes);
            long time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            fpsText.text = $"{1000 / (time - lastFrame)} ({watch.ElapsedMilliseconds} - {time - lastFrame})";
            lastFrame = time;
        }
        else
        {
            LoadImage(bytes);
        }
    }

    private void LoadImage(byte[] bytes)
    {
        cameraTexture.LoadImage(bytes);
    }

    public void SetScale(float scale)
    {
        string axis = (mapResolution / (500 * scale)).ToString("F1").TrimEnd('0').Replace(',', '.').TrimEnd('.') + "m";
        xAxis.text = axis;
        yAxis.text = axis;
        robot.sizeDelta = ScanOptions.robotSize * scale * robot.parent.GetComponent<RectTransform>().rect.size / mapResolution;
    }

    public void SetRobotPosition(Vector2 relativePosition, float orientation)
    {
        robot.localPosition = relativePosition * robot.parent.GetComponent<RectTransform>().rect.size / mapResolution - robot.parent.GetComponent<RectTransform>().rect.size / 2;
        robot.eulerAngles = new Vector3(0, 0, orientation);
    }

    public void SaveMap()
    {
        StartCoroutine(_SaveMap());
    }

    private IEnumerator _SaveMap()
    {
        isSaving = true;
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files);
        if (FileBrowser.Success)
        {
            Transform mapCopy = Instantiate(mainMap.GetChild(0), renderCamera.transform);
            Destroy(mapCopy.GetChild(1).gameObject);
            mapCopy.GetComponent<RawImage>().texture = mapTexture;
            mapCopy.GetChild(0).gameObject.SetActive(true);
            renderCamera.gameObject.SetActive(true);
            renderCamera.Render();
            renderCamera.gameObject.SetActive(false);
            Texture2D texture = new Texture2D(mapResolution, mapResolution);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, mapResolution, mapResolution), 0, 0);
            FileBrowserHelpers.WriteBytesToFile(FileBrowser.Result[0], texture.EncodeToPNG());
            Destroy(mapCopy.gameObject);
        }
        isSaving = false;
    }

    public void ShowConfirmExit()
    {
        exitPannel.parent.gameObject.SetActive(true);
        confirmingExit = true;
    }

    public void HideConfirmExit()
    {
        exitPannel.parent.gameObject.SetActive(false);
        confirmingExit = false;
    }

    public void Exit()
    {
        HideConfirmExit();
        Deactivate();
    }

    private Color32[] Interpolate(Color32 topLeft, Color32 topRight, Color32 bottomLeft, Color32 bottomRight, int width, int height)
    {
        //Debug.Log($"{topLeft} {topRight} {bottomLeft} {bottomRight}");
        Color32[] interpolation = new Color32[width * height];
        for (int i = 0; i < height; i++)
        {
            Color32 start = Color32.Lerp(bottomLeft, topLeft, (float)i / height);
            Color32 end = Color32.Lerp(bottomRight, topRight, (float)i / height);
            for(int j = 0; j < width; j++)
            {
                interpolation[width * i + j] = Color32.Lerp(start, end, (float)j / width);
            }
        }
        return interpolation;
    }
}
