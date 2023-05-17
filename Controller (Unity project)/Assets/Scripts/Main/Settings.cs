using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public static Settings instance { get; private set; }

    // Settings (saved)
    public static int language;
    public static Color color1;
    public static Color color2;
    public static float sensitivity;
    public static float speed;
    public static float precision;
    public static int controls;
    public static bool freezeStartPos;
    public static bool freezePos;
    public static bool moveCombination;
    public static bool speedChanges;
    public const int maxKeyboardChain = 2;

    // Objects
    public Transform generalPannel;
    public Transform controlledPannel;
    public ColorsPicker colorsPicker;
    public Dropdown languageDropdown;
    public Slider sensitivitySlider;
    public Slider speedSlider;
    public Slider precisionSlider;
    public Dropdown controlsDropdown;
    public Transform colorPicker;
    public Transform colorPickerPlaceholder;
    public GameObject controlsMovement;
    public GameObject freezePosToggle;
    public GameObject freezeStartPosToggle;
    public Toggle moveCombinationToggle;
    public Toggle speedChangesToggle;
    public Transform exitButton;

    // Others
    private string[] controlsOptions = new string[] { "Joystick", "Cursors", "Keyboard" };
    public static float scanPrecision;
    private float oldPrecision;

    private void Awake()
    {
        instance = this;
        Load();
        colorsPicker.startColor1 = color1;
        colorsPicker.startColor2 = color2;
        colorsPicker.colorPickerPos1 = colorPicker.position;
        colorsPicker.colorPickerPos2 = colorPickerPlaceholder.position;
    }

    private void Start()
    {
        languageDropdown.value = language;
        sensitivitySlider.value = sensitivity;
        speedSlider.value = speed;
        precisionSlider.value = 20 - precision;
        controlsDropdown.value = controls;
        freezePosToggle.GetComponent<Toggle>().isOn = freezePos;
        freezeStartPosToggle.GetComponent<Toggle>().isOn = freezeStartPos;
        moveCombinationToggle.isOn = moveCombination;
        speedChangesToggle.isOn = speedChanges;

        LanguageChange(false);
        SensitivityChange(false);
        ControlsChange(false);
        FreezeStartPosChange(false);

        generalPannel.localScale = Main.kyScale;
        generalPannel.position = new Vector3((Screen.width - 1440 * Main.ky) / 3.5f + 360 * Main.ky, Screen.height / 2, 0);
        controlledPannel.localScale = Main.kyScale;
        controlledPannel.position = new Vector3(Screen.width - (Screen.width - 1440 * Main.ky) / 3.5f - 360 * Main.ky, Screen.height / 2, 0);
        exitButton.localScale = Main.kyScale;
        exitButton.position = new Vector3(Screen.width / 2, 55 * Main.ky);

        gameObject.SetActive(false);
    }

    public static void Load()
    {
        language = PlayerPrefs.GetInt("language", 0);
        color1 = new Color(PlayerPrefs.GetFloat("color1r", 0), PlayerPrefs.GetFloat("color1g", 0), PlayerPrefs.GetFloat("color1b", 1));
        color2 = new Color(PlayerPrefs.GetFloat("color2r", 1), PlayerPrefs.GetFloat("color2g", 0), PlayerPrefs.GetFloat("color2b", 0));
        sensitivity = PlayerPrefs.GetFloat("sensitivity", 0);
        speed = PlayerPrefs.GetFloat("speed", 1.5f);
        precision = PlayerPrefs.GetFloat("precision", 10);
        controls = PlayerPrefs.GetInt("controls", Application.platform == RuntimePlatform.Android ? 0 : 2);
        freezePos = PlayerPrefs.GetInt("freeze", 0) == 1;
        freezeStartPos = PlayerPrefs.GetInt("freezeStart", 0) == 1;
        moveCombination = PlayerPrefs.GetInt("moveCombination", 1) == 1;
        speedChanges = PlayerPrefs.GetInt("speedChanges", 1) == 1;
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("language", language);
        PlayerPrefs.SetFloat("color1r", color1.r);
        PlayerPrefs.SetFloat("color1g", color1.g);
        PlayerPrefs.SetFloat("color1b", color1.b);
        PlayerPrefs.SetFloat("color2r", color2.r);
        PlayerPrefs.SetFloat("color2g", color2.g);
        PlayerPrefs.SetFloat("color2b", color2.b);
        PlayerPrefs.SetFloat("sensitivity", sensitivity);
        PlayerPrefs.SetFloat("speed", speed);
        PlayerPrefs.SetFloat("precision", precision);
        PlayerPrefs.SetInt("controls", controls);
        PlayerPrefs.SetInt("freeze", freezePos ? 1 : 0);
        PlayerPrefs.SetInt("freezeStart", freezeStartPos ? 1 : 0);
        PlayerPrefs.SetInt("moveCombination", moveCombination ? 1 : 0);
        PlayerPrefs.SetInt("speedChanges", speedChanges ? 1 : 0);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
        if (precision != oldPrecision)
        {
            oldPrecision = precision;
            if (MetalMap.instance.gameObject.activeSelf && MetalMap.instance.mode == 1)
            {
                Main.instance.SendInstruction("precision " + precision.ToString().Replace(',', '.'));
            }
        }
    }

    public void ColorChange(bool save)
    {
        color1 = colorsPicker.color1;
        color2 = colorsPicker.color2;
        MetalMap.instance.UpdateMap();
        if(save) Save();
    }

    public void LanguageChange(bool save)
    {
        language = languageDropdown.value;
        Translations.SetLanguage(language);
        Translations.UpdateTexts();
        for(int i = 0; i < controlsOptions.Length; i++)
        {
            controlsDropdown.options[i].text = Translations.Translate(controlsOptions[i]);
        }
        controlsDropdown.RefreshShownValue();
        if (save) Save();
    }

    public void SensitivityChange(bool save)
    {
        sensitivity = sensitivitySlider.value;
        colorsPicker.ChangeTicks(0, 1 - sensitivity);
        MetalMap.instance.UpdateMap();
        if (save) Save();
    }

    public void SpeedChange(bool save)
    {
        speed = speedSlider.value;
        if (save) Save();
    }

    public void PrecisionChange(bool save)
    {
        if(!Main.instance.demoBuild || !MetalMap.instance.gameObject.activeSelf)
        {
            precision = 20 - precisionSlider.value;
            if (save) Save();
        }
    }

    public void ControlsChange(bool save)
    {
        controls = controlsDropdown.value;
        controlsMovement.SetActive(controls != 2);
        MetalMap.instance.UpdateControls();
        if (save) Save();
    }

    public void FreezePosChange(bool save)
    {
        freezePos = freezePosToggle.GetComponent<Toggle>().isOn;
        MetalMap.instance.joystick.GetComponent<Cursor>().fixedPosition = freezePos;
        MetalMap.instance.forwardCursor.GetComponent<Cursor>().fixedPosition = freezePos;
        MetalMap.instance.directionCursor.GetComponent<Cursor>().fixedPosition = freezePos;
        if (save) Save();
    }

    public void FreezeStartPosChange(bool save)
    {
        freezeStartPos = freezeStartPosToggle.GetComponent<Toggle>().isOn;
        freezePosToggle.SetActive(!freezeStartPos);
        MetalMap.instance.joystick.GetComponent<Cursor>().fixedStartPosition = freezeStartPos;
        MetalMap.instance.forwardCursor.GetComponent<Cursor>().fixedStartPosition = freezeStartPos;
        MetalMap.instance.directionCursor.GetComponent<Cursor>().fixedStartPosition = freezeStartPos;
        if (freezeStartPos)
        {
            MetalMap.instance.joystick.GetComponent<Cursor>().fixedPosition = true;
            MetalMap.instance.forwardCursor.GetComponent<Cursor>().fixedPosition = true;
            MetalMap.instance.directionCursor.GetComponent<Cursor>().fixedPosition = true;
        }
        if (save) Save();
    }

    public void MoveCombinationChange(bool save)
    {
        moveCombination = moveCombinationToggle.isOn;
        if (save) Save();
    }

    public void SpeedChangesChange(bool save)
    {
        speedChanges = speedChangesToggle.isOn;
        if (save) Save();
    }
}
