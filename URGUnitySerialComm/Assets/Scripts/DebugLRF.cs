using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    Dropdown portList;

    InputField input;

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
    /// 
    /// </summary>
    /// <param name="str"></param>
    public void AdjustScanX(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.x = value;
        }
    }

    public void AdjustScanY(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.y = value;
        }
    }

    public void AdjustScanWidth(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.width = value;
        }
    }

    public void AdjustScanHeight(string str)
    {
        float value;
        if (float.TryParse(str, out value))
        {
            click.ScanRange.height = value;
        }
    }
}
