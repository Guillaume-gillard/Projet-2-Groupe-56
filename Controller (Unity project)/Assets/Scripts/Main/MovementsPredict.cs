using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public class MovementsPredict : MonoBehaviour
{
    public static MovementsPredict instance { get; private set; }

    private float orientation = 0;
    private Vector2 position;
    private Vector2 sensorPosition;

    public const float sensorOffset = 7.3f;
    
    // Demo
    private float[][] metalMap;
    private Vector2Int originCoords;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Main.instance.demoBuild)
        {
            if (MetalMap.instance.mode == 1)
            {
                if (Controls.instance.instruction.action != Instruction.Action.nothing)
                {
                    IncrementPosition(new Movement(Controls.instance.instruction, Time.deltaTime));
                }
            }
            else
            {
                IncrementPosition(SimulateMovement.Progress(Time.deltaTime));
            }
        }   
    }

    public void Restart()
    {
        orientation = 0;
        position = -new Vector2(-Mathf.Sin(orientation), Mathf.Cos(orientation)) * sensorOffset;
        sensorPosition = Vector2.zero;
        metalMap = new float[][] { new float[] { } };
        originCoords = Vector2Int.zero;
    }

    public void SetPosition(Vector2 _sensorPosition, float _orientation)
    {
        sensorPosition = _sensorPosition;
        position = _sensorPosition - new Vector2(-Mathf.Sin(orientation), Mathf.Cos(orientation)) * sensorOffset;
        orientation = _orientation * Mathf.Deg2Rad;
    }

    private void IncrementPosition(Movement movement)
    {
        if (movement.instruction.action == Instruction.Action.forward || movement.instruction.action == Instruction.Action.backward)
        {
            position = new Vector2(position.x - movement.instruction.GetForwardSpeed() * movement.time * Mathf.Sin(orientation), position.y + movement.instruction.GetForwardSpeed() * movement.time * Mathf.Cos(orientation));
        }
        else if (movement.instruction.action == Instruction.Action.right || movement.instruction.action == Instruction.Action.left)
        {
            orientation -= movement.instruction.GetRightAngularSpeed() * Mathf.Deg2Rad * movement.time;
        }
        else if (movement.instruction.action == Instruction.Action.combine)
        {
            float r = (Controls.distanceBetweenWheels / 2) * (movement.instruction.wheel1Speed + movement.instruction.wheel2Speed) / (movement.instruction.wheel1Speed - movement.instruction.wheel2Speed);
            float da = -movement.time * (movement.instruction.wheel1Speed + movement.instruction.wheel2Speed) / (2 * r);
            position = new Vector2(position.x + r * Mathf.Cos(orientation) - r * Mathf.Cos(orientation + da), position.y + r * Mathf.Sin(orientation) - r * Mathf.Sin(orientation + da));
            orientation += da;
        }
        sensorPosition = position + new Vector2(-Mathf.Sin(orientation), Mathf.Cos(orientation)) * sensorOffset;
        MetalMap.instance.SetRobotPosition(MetalMap.instance.originPos + sensorPosition * MetalMap.instance.scale, orientation * Mathf.Rad2Deg);

        if (Main.instance.demoBuild)
        {
            AddData();
        }
    }

    private void AddData()
    {
        float xtmp = sensorPosition.x / (MetalMap.instance.mode == 1 ? Settings.precision : Settings.scanPrecision) + 0.5f;
        float ytmp = sensorPosition.y / (MetalMap.instance.mode == 1 ? Settings.precision : Settings.scanPrecision) + 0.5f;
        int x = originCoords.x + (int)(xtmp > 0 ? xtmp : xtmp - 1);
        int y = originCoords.y + (int)(ytmp > 0 ? ytmp : ytmp - 1);
        while (x < 0)
        {
            x++;
            AddRowToBegin();
        }
        while (x >= metalMap.Length)
        {
            AddRowToEnd();
        }
        while (y < 0)
        {
            y++;
            AddColumnToBegin();
        }
        while (y >= metalMap[0].Length)
        {
            AddColumnToEnd();
        }

        if (metalMap[x][y] == -1)
        {
            List<float> values = new List<float>();
            int[,] directions = new int[,] { { -1, 0 }, { 0, 1 }, { 1, 0 }, { 0, -1 } };
            for (int i = 0; i < 4; i++)
            {
                if (x + directions[i, 0] >= 0 && x + directions[i, 0] < metalMap.Length && y + directions[i, 1] >= 0 && y + directions[i, 1] < metalMap[0].Length && metalMap[x + directions[i, 0]][y + directions[i, 1]] != -1)
                {
                    values.Add(metalMap[x + directions[i, 0]][y + directions[i, 1]]);
                }
            }
            if (values.Count == 0) values.Add(0.5f);
            metalMap[x][y] = values.Average() + UnityEngine.Random.value * 0.3f - 0.15f;
            MetalMap.instance.UpdateMap(MapData());
        }
    }

    private string MapData()
    {
        return $"{sensorPosition.x};{sensorPosition.y};{orientation};{originCoords.x};{originCoords.y};{JsonConvert.SerializeObject(metalMap)}";
    }

    private void AddRowToEnd()
    {
        float[][] newMetalMap = new float[metalMap.Length + 1][];
        for(int i = 0; i < metalMap.Length; i++)
        {
            newMetalMap[i] = metalMap[i];
        }
        newMetalMap[metalMap.Length] = Enumerable.Repeat(-1f, metalMap[0].Length).ToArray();
        metalMap = newMetalMap;
    }

    private void AddRowToBegin()
    {
        float[][] newMetalMap = new float[metalMap.Length + 1][];
        newMetalMap[0] = Enumerable.Repeat(-1f, metalMap[0].Length).ToArray();
        for (int i = 0; i < metalMap.Length; i++)
        {
            newMetalMap[i + 1] = metalMap[i];
        }
        metalMap = newMetalMap;
        originCoords.x++;
    }

    private void AddColumnToEnd()
    {
        float[][] newMetalMap = new float[metalMap.Length][];
        for (int i = 0; i < metalMap.Length; i++)
        {
            newMetalMap[i] = new float[metalMap[0].Length + 1];
            Array.Copy(metalMap[i], newMetalMap[i], metalMap[0].Length);
            newMetalMap[i][metalMap[0].Length] = -1;
        }
        metalMap = newMetalMap;
    }

    private void AddColumnToBegin()
    {
        float[][] newMetalMap = new float[metalMap.Length][];
        for (int i = 0; i < metalMap.Length; i++)
        {
            newMetalMap[i] = new float[metalMap[0].Length + 1];
            newMetalMap[i][0] = -1;
            Array.Copy(metalMap[i], 0, newMetalMap[i], 1, metalMap[0].Length);
        }
        metalMap = newMetalMap;
        originCoords.y++;
    }
}
