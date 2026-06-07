using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GridNode
{
    public int x;
    public int y;

    public float g;
    public float h;
    public float f;

    public Vector2 pos;

    public GridNode parent;
}
