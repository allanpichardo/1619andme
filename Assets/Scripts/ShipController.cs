using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public Vector3 pivotPoint;

    public float speed = 1.0f;
    public float bobHeight = 10.0f;
    public float bobAmount = 0.1f;

    // Update is called once per frame
    void Update()
    {
        float angle = Mathf.LerpAngle(0f, 359.9f, Time.deltaTime * speed);
        transform.RotateAround(pivotPoint, Vector3.up, angle);
    }
    
}
