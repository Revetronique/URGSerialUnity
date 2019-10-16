using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LRFClick : MonoBehaviour
{
    [System.Serializable]
    class ScanScreenPointEvent : UnityEngine.Events.UnityEvent<Vector2> { }

    /// <summary>
    /// containing scanned values from Laser Range Finder
    /// </summary>
    [SerializeField, Tooltip("LRF class instance to acquire scanned data")]
    URGSerial urg;

    /// <summary>
    /// physical size of scanned area with LRF
    /// </summary>
    [SerializeField, Tooltip("Physical size and origin (Bottom Right) of scanned area (mm)")]
    Rect scanRange = new Rect();

    /// <summary>
    /// Rectangle scanning range
    /// </summary>
    public Rect ScanRange { get { return scanRange; } }

    /// <summary>
    /// Conversion matrix for homography transformation
    /// </summary>
    public Matrix4x4 QuadWarp { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        var topLeft = new Vector2(scanRange.xMax, scanRange.yMax);
        var bottomLeft = new Vector2(scanRange.xMax, scanRange.yMin);
        var bottomRight = new Vector2(scanRange.xMin, scanRange.yMin);
        var topRight = new Vector2(scanRange.xMin, scanRange.yMax);
        
        QuadWarp = calcHomography(topLeft, bottomLeft, bottomRight, topRight).inverse;
    }

    /// <summary>
    /// Get screen points calculated from scanned points with LRF 
    /// </summary>
    /// <returns>list of screen position (x, y)</returns>
    public List<Vector2> GetScanScreenPoint()
    {
        var scanning = new List<Vector2>();

        foreach (var scan in urg.ScanPoints)
        {
            //convert coordination system from polar to orthogonal
            var pos = polarToOrth(scan.Key + 90, scan.Value);
            //only if the point in the range
            if (pos.x >= scanRange.xMin && pos.x <= scanRange.xMax && pos.y >= scanRange.yMin && pos.y <= scanRange.yMax)
            {
                //convert scanned point as screen position
                var quad = QuadWarp * new Vector4(pos.x, pos.y, 1, 0);
                var point = new Vector2(quad.x * Screen.width, quad.y * Screen.height);
                scanning.Add(point);
            }
        }

        return scanning;
    }

    /// <summary>
    /// <para>Calculate scanned points in orthogonal coordination system</para>
    /// <para>x-axis as horizontal, y-axis as vertical</para>
    /// </summary>
    /// <returns>List of detecting points (mm)</returns>
    public List<Vector2> GetScanPoint()
    {
        var points = new List<Vector2>();

        foreach (var scan in urg.ScanPoints)
        {
            // add results in the list
            // turn left to adjust the point at 90 degree as the center
            points.Add(polarToOrth(scan.Key + 90, scan.Value));
        }

        return points;
    }

    /// <summary>
    /// Convert 4 UV points({0,0},{1,0},{1,1},{0,1}) to arbitrary positions
    /// </summary>
    /// <param name="p0">Top Left</param>
    /// <param name="p1">Bottom Left</param>
    /// <param name="p2">Bottom Right</param>
    /// <param name="p3">Top Right</param>
    /// <returns>Converting Matrix (use only 3x3 elements)</returns>
    Matrix4x4 calcHomography(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var a = p3.x - p2.x;
        var b = p1.x - p2.x;
        var c = p0.x - p1.x + p2.x - p3.x;
        var d = p3.y - p2.y;
        var e = p1.y - p2.y;
        var f = p0.y - p1.y + p2.y - p3.y;

        var z = b * d - a * e;
        var g = (b * f - c * e) / z;
        var h = (c * d - a * f) / z;

        var system = new[]
        {
            p3.x * g - p0.x + p3.x,
            p1.x * h - p0.x + p1.x,
            p0.x,
            p3.y * g - p0.y + p3.y,
            p1.y * h - p0.y + p1.y,
            p0.y,
            g,
            h,
        };

        var mtx = Matrix4x4.identity;
        mtx.m00 = system[0]; mtx.m01 = system[1]; mtx.m02 = system[2];
        mtx.m10 = system[3]; mtx.m11 = system[4]; mtx.m12 = system[5];
        mtx.m20 = system[6]; mtx.m21 = system[7]; mtx.m22 = 1f;

        return mtx;
    }

    /// <summary>
    /// convert points in polar coordination to orthogonal
    /// </summary>
    /// <param name="angle">degree angle</param>
    /// <param name="distance">radius from origin</param>
    /// <returns></returns>
    Vector2 polarToOrth(float angle, long distance)
    {
        // convert the angle as radian
        var rad = angle / 180.0f * Mathf.PI;
        // convert points in polar coordination to orthogonal
        var x = distance * Mathf.Cos(rad);
        var y = distance * Mathf.Sin(rad);

        return new Vector2(x, y);
    }
}
