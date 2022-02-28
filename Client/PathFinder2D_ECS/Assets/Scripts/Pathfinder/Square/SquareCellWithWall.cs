﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WallType
{
    Wall,
    Door
}

public class SquareCellWithWall : SquareCell
{
    public Transform WallWhole;
    public Transform[] Walls;
    public Transform[] Doors;
    
    public void InitWall()
    {
        WallWhole.gameObject.SetActive(false);

        foreach (var wall in Walls)
        {
            wall.gameObject.SetActive(false);
        }

        foreach (var door in Doors)
        {
            door.gameObject.SetActive(false);
        }
    }

    public override void SetNeighbor(SquareDirection direction, SquareCell cell)
    {
        if(cell == null)
        {
            neighbors[(int)direction] = null;
        }
        else
        {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }
    }

    public void RefreshDirWall(SquareDirection dir)
    {
        Walls[(int)dir].gameObject.SetActive(false);
        Doors[(int)dir].gameObject.SetActive(false);
        
        if (neighbors[(int)dir] == null)
        {
            Walls[(int)dir].gameObject.SetActive(true);
        }
        else
        {
            Doors[(int)dir].gameObject.SetActive(true);
        }
    }

    public void RefreshIsBlock()
    {
        bool noNeighbor = true;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
            {
                noNeighbor = false;
                break;
            }
        }

        if (noNeighbor)
            WallWhole.gameObject.SetActive(true);
        else
            WallWhole.gameObject.SetActive(false);
    }

    public void RefreshEmpty(List<SquareDirection> ignoreDirections = null)
    {
        if (ignoreDirections == null)
            return;

        bool isEmpty = true;
        for (int i = 0; i < neighbors.Length; i++)
        {
            if(ignoreDirections.Contains((SquareDirection)i))
                continue;

            if (neighbors[i] == null)
            {
                isEmpty = false;
                break;
            }
        }

        if (isEmpty)
        {
            if(ignoreDirections == null || ignoreDirections.Count == 0)
            {
                foreach (var wall in Walls)
                {
                    wall.gameObject.SetActive(false);
                }
                foreach (var door in Doors)
                {
                    door.gameObject.SetActive(false);
                }
            }
            else
            {
                for (int i = 0; i < neighbors.Length; i++)
                {
                    if(neighbors[i] != null)
                    {
                        Walls[i].gameObject.SetActive(false);
                        Doors[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
