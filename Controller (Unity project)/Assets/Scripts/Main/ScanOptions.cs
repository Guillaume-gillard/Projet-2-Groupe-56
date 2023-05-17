using UnityEngine;
using UnityEngine.UI;

public class ScanOptions : MonoBehaviour
{
    public static ScanOptions instance { get; private set; }
    public Transform optionsPannel;
    public Slider widthSlider;
    public Slider heightSlider;
    public Slider precisionSlider;
    public RectTransform preview;
    public RectTransform zone;
    public Transform widthIndicator;
    public Transform heightIndicator;
    public RectTransform precisionIndicator;
    public RectTransform robot;
    public Transform linePrefab;

    private float width;
    private float height;
    private float precision;

    public readonly static Vector2 robotSize = new Vector2(25, 21.15f);
    private readonly static Vector2 previewBorders = new Vector2(Mathf.Sqrt(Mathf.Pow(robotSize.x / 2, 2) + Mathf.Pow(robotSize.y, 2)) - robotSize.x / 2, Mathf.Sqrt(Mathf.Pow(robotSize.x / 2, 2) + Mathf.Pow(robotSize.y, 2)));


    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        optionsPannel.localScale = Main.kyScale;
        optionsPannel.position = new Vector2(400 * Main.ky, Screen.height / 2);
        preview.localScale = Main.kyScale;

        Deactivate();
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        width = 200;
        height = 200;
        precision = 10;
        widthSlider.value = width;
        heightSlider.value = height;
        precisionSlider.value = precision;
        UpdatePreview();
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void ConfirmOptions()
    {
        Main.instance.SendInstruction($"scan {width} {height} {width / precision} {Settings.speed}".Replace(',', '.'));
        Settings.scanPrecision = width / precision;
        SimulateMovement.Reset();
        float speed = Settings.speed * Mathf.PI * Controls.wheelDiameter;
        float angularSpeed = 360 * Settings.speed * Controls.wheelDiameter / Controls.distanceBetweenWheels;
        for (int i = 0; i < precision; i++)
        {
            SimulateMovement.Forward(speed, height);
            SimulateMovement.Nothing(0.1f);

            if (i == precision - 1) break;

            if(i % 2 == 0) SimulateMovement.TurnRight(angularSpeed, 90);
            else SimulateMovement.TurnLeft(angularSpeed, 90);
            SimulateMovement.Nothing(0.1f);

            SimulateMovement.Forward(speed, width / precision);
            SimulateMovement.Nothing(0.1f);

            if (i % 2 == 0) SimulateMovement.TurnRight(angularSpeed, 90);
            else SimulateMovement.TurnLeft(angularSpeed, 90);
            SimulateMovement.Nothing(0.1f);
        }
        Deactivate();
        MetalMap.instance.Activate(0);
    }

    private void UpdatePreview()
    {
        preview.sizeDelta = new Vector2(900 * (width + 2 * previewBorders.x) / (height + 2 * previewBorders.y), 900);
        if (preview.sizeDelta.x > Screen.width - 950 * Main.ky)
        {
            preview.sizeDelta = new Vector2(Screen.width - 950 * Main.ky, (Screen.width - 950 * Main.ky) * (height + 2 * previewBorders.y) / (width + 2 * previewBorders.x));
        }
        preview.position = new Vector2(Screen.width - (50 + preview.sizeDelta.x / 2) * Main.ky, Screen.height / 2);
        zone.sizeDelta = new Vector2(preview.sizeDelta.x * (width / (width + 2 * previewBorders.x)), preview.sizeDelta.y * (height / (height + 2 * previewBorders.y)));
        widthIndicator.position = new Vector2(widthIndicator.position.x, preview.position.y - (20 + preview.sizeDelta.y / 2) * Main.ky);
        heightIndicator.position = new Vector2(preview.position.x - (20 + preview.sizeDelta.x / 2) * Main.ky, heightIndicator.position.y);
        widthIndicator.GetChild(2).GetComponent<Text>().text = Mathf.Round(width) + " cm";
        heightIndicator.GetChild(2).GetComponent<Text>().text = Mathf.Round(height) + " cm";
        precisionIndicator.sizeDelta = new Vector2(zone.sizeDelta.x / precision - 36, precisionIndicator.sizeDelta.y);
        precisionIndicator.position = new Vector2(preview.position.x - ((1 - 1 / precision) * zone.sizeDelta.x / 2) * Main.ky, preview.position.y + (20 + preview.sizeDelta.y / 2) * Main.ky);
        precisionIndicator.GetChild(2).GetComponent<Text>().text = (width / precision).ToString("F1").TrimEnd('0').Replace(',', '.').TrimEnd('.') + " cm";
        robot.sizeDelta = new Vector2(robotSize.x * zone.sizeDelta.x / width, robotSize.y * zone.sizeDelta.y / height);
        robot.position = new Vector2(precisionIndicator.position.x, preview.position.y - zone.sizeDelta.y * Main.ky / 2);
        
        float distance = 0;
        float lineSize = 12 * zone.sizeDelta.y * Settings.speed / height;
        float emptySize = 8 * zone.sizeDelta.y * Settings.speed / height;
        float remaining = 0;
        bool up = true;
        int nbrLinesBefore = zone.childCount - 4;
        int count = 0;
        for(int i = 0; i < precision; i++)
        {
            while(distance < zone.sizeDelta.y - 26)
            {
                if(remaining > 0 && remaining < emptySize)
                {
                    distance = remaining;
                    remaining = 0;
                }

                Transform line;
                if (nbrLinesBefore > count) line = zone.GetChild(4 + count);
                else line = Instantiate(linePrefab, zone); 
                count++;

                if (up)
                {
                    line.localPosition = new Vector2(((0.5f + i) / precision - 0.5f) * zone.sizeDelta.x, distance - (zone.sizeDelta.y - 26) / 2);
                    line.rotation = Quaternion.identity;
                }
                else
                {
                    line.localPosition = new Vector2(((0.5f + i) / precision - 0.5f) * zone.sizeDelta.x, (zone.sizeDelta.y - 26) / 2 - distance);
                    line.rotation = new Quaternion(0, 0, 1, 0);
                }

                if (remaining == 0)
                {
                    if (distance + lineSize > zone.sizeDelta.y - 26) line.localScale = new Vector3(1, ((zone.sizeDelta.y - 26) - distance) / 100, 1);
                    else line.localScale = new Vector3(1, lineSize / 100, 1);
                    distance += lineSize + emptySize;
                }
                else
                {
                    line.localScale = new Vector3(1, (remaining - emptySize) / 100, 1);
                    distance += remaining;
                    remaining = 0;
                }
            }

            remaining = distance - (zone.sizeDelta.y - 26);
            distance = 0;
            up = !up;
        }

        for(int i = count; i < nbrLinesBefore; i++)
        {
            Destroy(zone.GetChild(i + 4).gameObject);
        }
    }

    public void WidthChange()
    {
        width = widthSlider.value;
        UpdatePreview();
    }

    public void HeightChange()
    {
        height = heightSlider.value;
        UpdatePreview();
    }

    public void PrecisionChange()
    {
        precision = precisionSlider.value;
        UpdatePreview();
    }
}
