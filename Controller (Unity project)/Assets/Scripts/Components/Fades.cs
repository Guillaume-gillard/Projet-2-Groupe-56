using UnityEngine;
using UnityEngine.UI;

public class Fades : MonoBehaviour
{
    private int type;
    private float timeSinceStartFade = 0;
    private float timeToFade = 0;
    private float delayBeforeFade = 0;
    private bool fadeIn;
    private bool deactivate;
    private Color color;

    private void Start()
    {
        if (GetComponent<Text>() != null) type = 1;
        else if (GetComponent<Image>() != null) type = 2;
        else if (GetComponent<SpriteRenderer>() != null) type = 3;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (timeToFade > 0 && delayBeforeFade == 0)
        {
            timeSinceStartFade += Time.deltaTime;
            if (timeSinceStartFade > timeToFade) timeSinceStartFade = timeToFade;
            switch (type)
            {
                case 1:
                    color = new Color(gameObject.GetComponent<Text>().color.r, gameObject.GetComponent<Text>().color.g, gameObject.GetComponent<Text>().color.b, fadeIn ? (timeSinceStartFade / timeToFade) : 1 - (timeSinceStartFade / timeToFade));
                    gameObject.GetComponent<Text>().color = color;  
                    break;
                case 2:
                    color = new Color(gameObject.GetComponent<Image>().color.r, gameObject.GetComponent<Image>().color.g, gameObject.GetComponent<Image>().color.b, fadeIn ? (timeSinceStartFade / timeToFade) : 1 - (timeSinceStartFade / timeToFade));
                    gameObject.GetComponent<Image>().color = color;
                    break;
                case 3:
                    color = new Color(gameObject.GetComponent<SpriteRenderer>().color.r, gameObject.GetComponent<SpriteRenderer>().color.g, gameObject.GetComponent<SpriteRenderer>().color.b, fadeIn ? (timeSinceStartFade / timeToFade) : 1 - (timeSinceStartFade / timeToFade));
                    gameObject.GetComponent<SpriteRenderer>().color = color;
                    break;
                default:
                    Debug.LogError("Type is incorrect !");
                    break;
            }

            if(timeSinceStartFade == timeToFade)
            {
                timeSinceStartFade = 0;
                timeToFade = 0;
            }
        }

        if (delayBeforeFade > 0)
        {
            delayBeforeFade -= Time.deltaTime;
            if (delayBeforeFade < 0) delayBeforeFade = 0;
        }

        if (color.a == 0 && !fadeIn && deactivate)
        {
            gameObject.SetActive(false);
        }
    }

    public void StartFadeIn(float time, float delay = 0)
    {
        delayBeforeFade = delay;
        timeToFade = time;
        fadeIn = true;
        timeSinceStartFade = 0;
        color = ObjectColor();
    }

    public void StartFadeOut(float time, float delay = 0, bool deactivateWhenDone = true)
    {
        delayBeforeFade = delay;
        timeToFade = time;
        fadeIn = false;
        deactivate = deactivateWhenDone;
        timeSinceStartFade = 0;
        color = ObjectColor();
    }

    private Color ObjectColor()
    {
        Color _color = new Color(0, 0, 0, 0);
        switch (type)
        {
            case 1:
                _color = gameObject.GetComponent<Text>().color;
                break;
            case 2:
                _color = gameObject.GetComponent<Image>().color;
                break;
            case 3:
                _color = gameObject.GetComponent<SpriteRenderer>().color;
                break;
            default:
                Debug.LogError("Type is incorrect !");
                break;
        }
        return _color;
    }
}
