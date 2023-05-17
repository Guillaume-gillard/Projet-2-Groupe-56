using UnityEngine;

public class Controls : MonoBehaviour
{
    public static Controls instance { get; private set; }

    public GameObject forwardCursor;
    public GameObject directionCursor;
    public GameObject joystick;

    public Instruction instruction { get; private set; } = Instruction.Nothing();
    private Instruction lastInstruction = Instruction.Nothing();
    private float instructionTimer;
    private const float chainTime = 0.12f;
    private int forwardChain;
    private float forwardTimer;
    private int lastForwardChain;
    private int backwardChain;
    private float backwardTimer;
    private int lastBackwardChain;
    private int rightChain;
    private float rightTimer;
    private int lastRightChain;
    private int leftChain;
    private float leftTimer;
    private int lastLeftChain;

    public const float wheelDiameter = 6.2f;
    public const float distanceBetweenWheels = 23.5f;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (forwardTimer > 0)
        {
            forwardTimer -= Time.deltaTime;
            if (forwardTimer < 0) forwardTimer = 0;
        }
        if (backwardTimer > 0)
        {
            backwardTimer -= Time.deltaTime;
            if (backwardTimer < 0) backwardTimer = 0;
        }
        if (rightTimer > 0)
        {
            rightTimer -= Time.deltaTime;
            if (rightTimer < 0) rightTimer = 0;
        }
        if (leftTimer > 0)
        {
            leftTimer -= Time.deltaTime;
            if (leftTimer < 0) leftTimer = 0;
        }

        if (MetalMap.instance.mode == 1 && !Settings.instance.gameObject.activeSelf && !MetalMap.instance.isSaving && !MetalMap.instance.exitPannel.parent.gameObject.activeSelf)
        {
            instructionTimer += Time.deltaTime;

            Vector2 movements = Vector2.zero;
            float norm = 1;

            if (Settings.controls == 0)
            {
                Vector2 value = joystick.GetComponent<Cursor>().vectorValue;
                value.y = Mathf.Round(value.y * 12 / Mathf.PI) * Mathf.PI / 12;
                movements = new Vector2(Mathf.Sin(value.y), Mathf.Cos(value.y));
                norm = value.x;
            }

            else if (Settings.controls == 1)
            {
                movements = new Vector2(forwardCursor.GetComponent<Cursor>().value, directionCursor.GetComponent<Cursor>().value);
                norm = (Mathf.Abs(movements.x) > Mathf.Abs(movements.y)) ? Mathf.Abs(movements.x) : Mathf.Abs(movements.y);
            }

            else if (Settings.controls == 2)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (forwardTimer > 0)
                    {
                        forwardChain = lastForwardChain + 1;
                        if (forwardChain > Settings.maxKeyboardChain) forwardChain = Settings.maxKeyboardChain;
                    }
                    else forwardChain = 1;
                }
                if (Input.GetKeyUp(KeyCode.UpArrow))
                {
                    lastForwardChain = forwardChain;
                    forwardChain = 0;
                    forwardTimer = chainTime;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (backwardTimer > 0)
                    {
                        backwardChain = lastBackwardChain + 1;
                        if (backwardChain > Settings.maxKeyboardChain) backwardChain = Settings.maxKeyboardChain;
                    }
                    else backwardChain = 1;
                }
                if (Input.GetKeyUp(KeyCode.DownArrow))
                {
                    lastBackwardChain = backwardChain;
                    backwardChain = 0;
                    backwardTimer = chainTime;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    if (rightTimer > 0)
                    {
                        rightChain = lastRightChain + 1;
                        if (rightChain > Settings.maxKeyboardChain) rightChain = Settings.maxKeyboardChain;
                    }
                    else rightChain = 1;
                }
                if (Input.GetKeyUp(KeyCode.RightArrow))
                {
                    lastRightChain = rightChain;
                    rightChain = 0;
                    rightTimer = chainTime;
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    if (leftTimer > 0)
                    {
                        leftChain = lastLeftChain + 1;
                        if (leftChain > Settings.maxKeyboardChain) leftChain = Settings.maxKeyboardChain;
                    }
                    else leftChain = 1;
                }
                if (Input.GetKeyUp(KeyCode.LeftArrow))
                {
                    lastLeftChain = leftChain;
                    leftChain = 0;
                    leftTimer = chainTime;
                }

                movements = new Vector2(forwardChain - backwardChain, rightChain - leftChain);
                norm = (Mathf.Abs(movements.x) > Mathf.Abs(movements.y)) ? (Mathf.Abs(movements.x) / Settings.maxKeyboardChain) : (Mathf.Abs(movements.y) / Settings.maxKeyboardChain);
            }

            if (!Settings.moveCombination)
            {
                if (Mathf.Abs(movements.x) >= Mathf.Abs(movements.y)) movements.y = 0;
                else movements.x = 0;
            }
            if (movements != Vector2.zero) movements /= Mathf.Abs(movements.x) + Mathf.Abs(movements.y);
            if (Settings.speedChanges) movements *= norm;

            if (movements == Vector2.zero) instruction = Instruction.Nothing();
            else if (Mathf.Abs(movements.y) < 0.0001f) instruction = movements.x > 0 ? Instruction.Forward(movements.x * Settings.speed * Mathf.PI * wheelDiameter) : Instruction.Backward(-movements.x * Settings.speed * Mathf.PI * wheelDiameter);
            else if (Mathf.Abs(movements.x) < 0.0001f) instruction = movements.y > 0 ? Instruction.Right(360 * movements.y * Settings.speed * wheelDiameter / distanceBetweenWheels) : Instruction.Left(-360 * movements.y * Settings.speed * wheelDiameter / distanceBetweenWheels);
            else instruction = Instruction.Combine((movements.x + movements.y) * Settings.speed * Mathf.PI * wheelDiameter, (movements.x - movements.y) * Settings.speed * Mathf.PI * wheelDiameter);

            if (instructionTimer > 0.8 || !Instruction.AlmostEqual(instruction, lastInstruction))
            {
                instructionTimer = 0;
                lastInstruction = instruction;
                Main.instance.SendInstruction(instruction.ToString().Replace(',', '.'));
            }
        }
    }
}
