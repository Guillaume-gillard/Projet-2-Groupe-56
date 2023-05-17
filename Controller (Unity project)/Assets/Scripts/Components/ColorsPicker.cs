using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ColorsPicker : MonoBehaviour
{
    public Color color1 { get; private set; }
    public Color color2 { get; private set; }

    private int pickerModified;

    // Objects
    public Image picker1; // The image that displays the first color
    public Image picker2; // The image that displays the second color
    public GameObject colorPicker; // The color picker object (Asset: ColorGradientPicker)
    public Material gradientMaterial;
    public Transform ticks;

    // Settings
    public Color startColor1;
    public Color startColor2;
    public Vector2 colorPickerPos1;
    public Vector2 colorPickerPos2;
    public string materialColor1;
    public string materialColor2;
    public int nbrTicks;
    public UnityEvent onColorChange;

    private void Awake()
    {
        for (int i = 0; i < nbrTicks - 1; i++)
        {
            Instantiate(ticks.GetChild(0).gameObject, ticks);
        }
    }

    private void Start()
    {
        picker1.color = startColor1;
        picker2.color = startColor2;
        color1 = startColor1;
        color2 = startColor2;
        gradientMaterial.SetColor(materialColor1, color1);
        gradientMaterial.SetColor(materialColor2, color2);

        for (int i = 0; i < nbrTicks; i++)
        {
            ticks.GetChild(i).position = new Vector3(picker1.transform.position.x + (picker2.transform.position.x - picker1.transform.position.x) * i / (nbrTicks - 1), ticks.GetChild(0).position.y, 0);
        }
    }

    public void PickColor1()
    {
        ColorPicker.Done();
        colorPicker.transform.position = colorPickerPos1;
        pickerModified = 1;
        ColorPicker.Create(color1, "Pick start color", ColorChange, ConfirmColorChange);
    }

    public void PickColor2()
    {
        ColorPicker.Done();
        colorPicker.transform.position = colorPickerPos2;
        pickerModified = 2;
        ColorPicker.Create(color2, "Pick end color", ColorChange, ConfirmColorChange);
    }

    private void ColorChange(Color color)
    {
        if(pickerModified == 1)
        {
            color1 = color;
            picker1.color = color;
            gradientMaterial.SetColor(materialColor1, color);
        }
        else if(pickerModified == 2)
        {
            color2 = color;
            picker2.color = color;
            gradientMaterial.SetColor(materialColor2, color);
        }
    }

    private void ConfirmColorChange(Color color)
    {
        onColorChange.Invoke();
    }

    public void ChangeTicks(float start, float end)
    { 
        for(int i = 0; i < nbrTicks; i++)
        {
            ticks.GetChild(i).gameObject.GetComponent<Text>().text = (start + (end - start) * i / (nbrTicks - 1)).ToString("F2").TrimEnd('0').Replace(',', '.').TrimEnd('.');
        }
    }
}
