﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <para>Visualizing scanned data from LRF</para>
/// <para>Scale Unit: meter (same as the default Unity scale)</para>
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LRFVisual : MonoBehaviour
{
    public enum ScaleUnit
    {
        METER,
        CENTIMETER,
        MILLIMETER,
        INCH
    }

    /// <summary>
    /// scan data of LRF
    /// </summary>
    [SerializeField]
    LRFClick lrf;

    /// <summary>
    /// Unit of the parameter to visualize
    /// </summary>
    public ScaleUnit unit = ScaleUnit.MILLIMETER;

    /// <summary>
    /// Visualizing the captured data from LRF
    /// </summary>
    LineRenderer visual;

    /// <summary>
    /// Scale ratio converting to meter in each unit
    /// </summary>
    float scaling
    {
        get
        {
            switch (unit)
            {
                case ScaleUnit.METER:
                    return 1;
                case ScaleUnit.CENTIMETER:
                    return 0.1f;
                case ScaleUnit.MILLIMETER:
                    return 0.001f;
                case ScaleUnit.INCH:
                    return 0.0254f;
                default:
                    return 1;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //get necessary component to visualize
        visual = GetComponent<LineRenderer>();
    }

    void OnDrawGizmos()
    {
        // visualize detecting range in Unity Editor
        // set the drawing color as red
        Gizmos.color = Color.red;
        // calculate the edge points of detecting range with considering the device posture
        var p1 = transform.position + transform.rotation * new Vector3(lrf.ScanRange.xMin, 0, lrf.ScanRange.yMin) * scaling;
        var p2 = transform.position + transform.rotation * new Vector3(lrf.ScanRange.xMin, 0, lrf.ScanRange.yMax) * scaling;
        var p3 = transform.position + transform.rotation * new Vector3(lrf.ScanRange.xMax, 0, lrf.ScanRange.yMax) * scaling;
        var p4 = transform.position + transform.rotation * new Vector3(lrf.ScanRange.xMax, 0, lrf.ScanRange.yMin) * scaling;
        // draw the detecting range with 4 lines
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }

    // Update is called once per frame
    void Update()
    {
        //get all scanned points
        var points = lrf.GetScanPoint();

        //set the total number of the points in LineRendere
        visual.positionCount = points.Count;
        //draw scanning result
        for (int i = 0; i < points.Count; i++)
        {
            var scan = new Vector3(points[i].x, 0, points[i].y) * scaling;
            visual.SetPosition(i, scan);
        }
    }
}