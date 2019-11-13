using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;

public class DebugLRF : MonoBehaviour
{
    [SerializeField]
    URGSerial urg;

    [SerializeField]
    LRFClick click;

    [SerializeField]
    LRFVisual visual;

    [SerializeField]
    Dropdown portList;

    // Start is called before the first frame update
    void Start()
    {
        UpdateSerialPort();
    }

    public void UpdateSerialPort()
    {
        portList.ClearOptions();
        //create list of port name
        var list = new List<string>();
        foreach (var item in SerialPort.GetPortNames())
        {
            list.Add(item);
        }
        portList.AddOptions(list);
        //set default value
        portList.value = 0;
    }

    /// <summary>
    /// Select COM port for serial communication with order
    /// </summary>
    /// <param name="num">index</param>
    public void SelectSerialPort(int num)
    {
        var ports = SerialPort.GetPortNames();
        urg.ChangePort(ports[num]);
    }

    /// <summary>
    /// Set up the coordinate system of visualization
    /// </summary>
    /// <param name="num">index (0:world, 1:screen)</param>
    public void ChangeVisualCoord(int num)
    {
        var values = System.Enum.GetValues(typeof(LRFVisual.Coordination));
        if (num < values.Length)
        {
            visual.Space = (LRFVisual.Coordination)values.GetValue(num);
        }
    }

    /// <summary>
    /// Set the right point of scanning range
    /// </summary>
    /// <param name="str">right(mm)</param>
    public void AdjustScanX(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.x = value;
        }
    }

    /// <summary>
    /// Set the top point of scanning range
    /// </summary>
    /// <param name="str">top(mm)</param>
    public void AdjustScanY(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.y = value;
        }
    }

    /// <summary>
    /// Set the width of scanning range
    /// </summary>
    /// <param name="str">width(mm)</param>
    public void AdjustScanWidth(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.width = value;
        }
    }

    /// <summary>
    /// Set the height of scanning range
    /// </summary>
    /// <param name="str">height(mm)</param>
    public void AdjustScanHeight(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.height = value;
        }
    }

    public void FlipLRFHorizontal(bool flip)
    {
        click.FlipHorizontal = flip;
    }

    public void FlipLRFVertical(bool flip)
    {
        click.FlipVertical = flip;
    }

    public void RemapQuad()
    {
        click.RemapQuadWarp();
    }
}
