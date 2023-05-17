using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    private static List<int> allTouchIDs = new List<int>();
    private Vector3 startLocalPosition;
    private Vector3 currentStartPosition;
    private int touchID = -1;

    public float value = 0;
    public Vector2 vectorValue = Vector2.zero; // (value, angle)

    // Settings
    public GameObject movingPart; // The object that moves inside the cursor
    public Vector2 startPosition; // The rest position of the cursor
    public Rect startTouchArea; // The area in which touches will start movement
    public enum CursorType
    {
        HorizontalSlide,
        VerticalSlide,
        Joystick
    }
    public CursorType cursorType = new CursorType();
    public int maxMovement; // The max ammount of pixels the cursor can move in each direction
    public bool fixedPosition; // Wether the cursor moves to the position of the touch when movement starts or not
    public bool fixedStartPosition; // Wether the cursor moves if the touch goes out of bounds or not
    public int width; // The width of the cursor (not for joysticks or fixed position cursors)

    private void Update()
    {
        bool found = false;
        for(int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touchID == -1 && !allTouchIDs.Contains(touch.fingerId) && touch.phase == TouchPhase.Began && startTouchArea.Contains(touch.position))
            {
                // Start movement
                found = true;
                touchID = touch.fingerId;
                allTouchIDs.Add(touchID);
                if (!fixedStartPosition) transform.position = touch.position;
                currentStartPosition = movingPart.transform.position;
                startLocalPosition = movingPart.transform.localPosition;
                break;
            }
            else if(touch.fingerId == touchID)
            {
                // Continue movement
                found = true;
                if(cursorType == CursorType.HorizontalSlide)
                {
                    value = (touch.position.x - currentStartPosition.x) / maxMovement;
                    if (value > 1) value = 1;
                    if (value < -1) value = -1;
                    vectorValue = new Vector2(value, 0);
                    movingPart.transform.position = new Vector3(currentStartPosition.x + maxMovement * value, currentStartPosition.y, currentStartPosition.z);
                    
                    if (!fixedPosition)
                    {
                        Vector3 addPosition = Vector3.zero;
                        if (touch.position.x > currentStartPosition.x + maxMovement) addPosition.x = touch.position.x - currentStartPosition.x - maxMovement;
                        else if (touch.position.x < currentStartPosition.x - maxMovement) addPosition.x = touch.position.x - currentStartPosition.x + maxMovement;
                        if (touch.position.y > currentStartPosition.y + width / 2) addPosition.y = touch.position.y - currentStartPosition.y - width / 2;
                        else if (touch.position.y < currentStartPosition.y - width / 2) addPosition.y = touch.position.y - currentStartPosition.y + width / 2;
                        transform.position += addPosition;
                        currentStartPosition += addPosition;
                    }
                }
                else if(cursorType == CursorType.VerticalSlide)
                {
                    value = (touch.position.y - currentStartPosition.y) / maxMovement;
                    if (value > 1) value = 1;
                    if (value < -1) value = -1;
                    vectorValue = new Vector2(value, 0);
                    movingPart.transform.position = new Vector3(currentStartPosition.x, currentStartPosition.y + maxMovement * value, currentStartPosition.z);

                    if (!fixedPosition)
                    {
                        Vector3 addPosition = Vector3.zero;
                        if (touch.position.y > currentStartPosition.y + maxMovement) addPosition.y = touch.position.y - currentStartPosition.y - maxMovement;
                        else if (touch.position.y < currentStartPosition.y - maxMovement) addPosition.y = touch.position.y - currentStartPosition.y + maxMovement;
                        if (touch.position.x > currentStartPosition.x + width / 2) addPosition.x = touch.position.x - currentStartPosition.x - width / 2;
                        else if (touch.position.x < currentStartPosition.x - width / 2) addPosition.x = touch.position.x - currentStartPosition.x + width / 2;
                        transform.position += addPosition;
                        currentStartPosition += addPosition;
                    }
                }
                else if (cursorType == CursorType.Joystick)
                {
                    Vector2 coords = (touch.position - (Vector2)currentStartPosition) / maxMovement;
                    if (coords.magnitude > 1)
                    {
                        if (!fixedPosition)
                        {
                            transform.position += (Vector3)(coords - coords.normalized) * maxMovement;
                            currentStartPosition += (Vector3)(coords - coords.normalized) * maxMovement;
                        }
                        coords = coords.normalized;
                    }
                    value = coords.magnitude;
                    vectorValue = new Vector2(value, Mathf.Atan2(coords.y, coords.x));
                    movingPart.transform.position = currentStartPosition + (Vector3)coords * maxMovement;
                }
                break;
            }
        }
        if (touchID != -1 && !found)
        {
            StopMovement();
        }
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void StopMovement()
    {
        value = 0;
        vectorValue = Vector2.zero;
        movingPart.transform.localPosition = startLocalPosition;
        allTouchIDs.Remove(touchID);
        touchID = -1;
        transform.position = startPosition;
    }
}
