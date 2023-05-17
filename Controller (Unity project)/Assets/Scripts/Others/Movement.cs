using System.Collections.Generic;
using UnityEngine;

public struct Instruction
{
    public enum Action
    {
        forward,
        backward,
        left,
        right,
        combine,
        nothing
    }

    public readonly Action action;
    public readonly float speed;
    public readonly float angularSpeed;
    public readonly float wheel1Speed;
    public readonly float wheel2Speed;

    private Instruction(Action _action, float _speed, float _angularSpeed, float _wheel1Speed, float _wheel2Speed)
    {
        action = _action;
        speed = _speed;
        angularSpeed = _angularSpeed;
        wheel1Speed = _wheel1Speed;
        wheel2Speed = _wheel2Speed;
    }

    public static Instruction Nothing()
    {
        return new Instruction(Action.nothing, 0, 0, 0, 0);
    }

    public static Instruction Forward(float speed)
    {
        return new Instruction(Action.forward, speed, 0, 0, 0);
    }

    public static Instruction Backward(float speed)
    {
        return new Instruction(Action.backward, speed, 0, 0, 0);
    }

    public static Instruction Left(float angularSpeed)
    {
        return new Instruction(Action.left, 0, angularSpeed, 0, 0);
    }

    public static Instruction Right(float angularSpeed)
    {
        return new Instruction(Action.right, 0, angularSpeed, 0, 0);
    }

    public static Instruction Combine(float wheel1Speed, float wheel2Speed)
    {
        return new Instruction(Action.combine, 0, 0, wheel1Speed, wheel2Speed);
    }

    public override string ToString()
    {
        if (action == Action.forward || action == Action.backward) return $"{action} {speed}";
        else if (action == Action.left || action == Action.right) return $"{action} {angularSpeed}";
        else if (action == Action.combine) return $"combine {wheel1Speed} {wheel2Speed}";
        else return "nothing";
    }

    public static bool AlmostEqual(Instruction instruction1, Instruction instruction2)
    {
        return instruction1.action == instruction2.action && (
            ((instruction1.action == Action.forward || instruction1.action == Action.backward) && Mathf.Abs(instruction1.speed - instruction2.speed) < 2.5f)
            || ((instruction1.action == Action.right || instruction1.action == Action.left) && Mathf.Abs(instruction1.angularSpeed - instruction2.angularSpeed) < 15)
            || (instruction1.action == Action.combine && Mathf.Abs(instruction1.wheel1Speed - instruction2.wheel1Speed) + Mathf.Abs(instruction1.wheel2Speed - instruction2.wheel2Speed) < 2.5f)
            || instruction1.action == Action.nothing
        );
    }

    public float GetForwardSpeed()
    {
        return action == Action.forward ? speed : -speed;
    }

    public float GetRightAngularSpeed()
    {
        return action == Action.right ? angularSpeed : -angularSpeed;
    }
}


public struct Movement
{
    public readonly Instruction instruction;
    public readonly float time;

    public Movement(Instruction _instruction, float _time)
    {
        instruction = _instruction;
        time = _time;
    }
}


public class SimulateMovement
{
    private static List<Movement> plannedMovements = new List<Movement>();
    private static float passedTime = 0;

    public static void Reset()
    {
        plannedMovements.Clear();
    }

    public static void Nothing(float time)
    {
        plannedMovements.Add(new Movement(Instruction.Nothing(), time));
    }

    public static void Forward(float speed, float distance)
    {
        plannedMovements.Add(new Movement(Instruction.Forward(speed), distance / speed));
    }

    public static void Backward(float speed, float distance)
    {
        plannedMovements.Add(new Movement(Instruction.Backward(speed), distance / speed));
    }

    public static void TurnLeft(float angularSpeed, float angle)
    {
        plannedMovements.Add(new Movement(Instruction.Left(angularSpeed), angle / angularSpeed));
    }

    public static void TurnRight(float angularSpeed, float angle)
    {
        plannedMovements.Add(new Movement(Instruction.Right(angularSpeed), angle / angularSpeed));
    }

    public static Movement Progress(float time)
    {
        if (plannedMovements.Count > 0)
        {
            passedTime += time;
            if (passedTime < plannedMovements[0].time)
            {
                return new Movement(plannedMovements[0].instruction, time);
            }
            else
            {
                Movement result = new Movement(plannedMovements[0].instruction, time + plannedMovements[0].time - passedTime);
                plannedMovements.RemoveAt(0);
                passedTime = 0;
                return result;
            }
        }
        else
        {
            return new Movement(Instruction.Nothing(), time);
        }
    }
}