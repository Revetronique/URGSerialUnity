using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using UnityEngine;
using URG;

public class URGSerial : MonoBehaviour
{
    public enum BAUDRATE
    {
        Default_19k2 = 19200,
        Fast_38k4 = 38400,
        High_57k6 = 57600,
        Full_115k2 = 115200,
    }
    
    /// <summary>
    /// Connecting destination
    /// </summary>
    [SerializeField, Tooltip("Connecting destination")]
    string port = "COM3";
    
    /// <summary>
    /// Communication speed (Baud Rate)
    /// </summary>
    [SerializeField, Tooltip("Communication speed (Baud Rate)")]
    BAUDRATE bps = BAUDRATE.Default_19k2;
    
    /// <summary>
    /// range of steps to measure distance
    /// </summary>
    [SerializeField, Tooltip("range of steps to measure distance (X:min, Y:max)")]
    Vector2 rangeStep = new Vector2(UtilitySCIP.StepMin, UtilitySCIP.StepMax);

    /// <summary>
    /// use short range mode?
    /// </summary>
    [SerializeField, Tooltip("use short range mode?")]
    protected bool isShortRange = false;

    /// <summary>
    /// how many angle steps does the system put together as one result.
    /// </summary>
    [SerializeField, Range(1, 32), Tooltip("how many angle steps does the system put together as one result.")]
    int group = 1;

    /// <summary>
    /// how many times does the system skip processing scan results.
    /// </summary>
    [SerializeField, Range(0, 10), Tooltip("how many times does the system skip processing scan results.")]
    int skip = 0;
    
    protected SerialPort serialPort;

    protected Dictionary<float, long> points = new Dictionary<float, long>();
    /// <summary>
    /// Distance at each angle (degree, mm)
    /// </summary>
    public Dictionary<float, long> ScanPoints { get { return points; } }

    // Start is called before the first frame update
    void Start()
    {
        //connection setting
        serialPort = new SerialPort(port, (int)bps);
        //default parameters
        serialPort.Parity = Parity.None;
        serialPort.DataBits = 8;
        serialPort.StopBits = StopBits.One;
        serialPort.Handshake = Handshake.None;
        //terminal symbol (End Line)
        serialPort.NewLine = "\n\n";

        //start up Serial Port
        OnEnable();
    }

    protected virtual void OnEnable()
    {
        //start up Serial Port
        if (Open())
        {
            //start scanning
            StartScan();
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (serialPort.IsOpen)
        {
            //time stamp
            long timeStamp = 0;

            if (UtilitySCIP.ProcessReadDistance(serialPort.ReadLine(), out points, ref timeStamp, true, isShortRange))
            {
#if UNITY_EDITOR
                foreach (var point in points)
                {
                    Debug.LogFormat("Angle: {0}(deg), Distance: {1}(mm)", point.Key, point.Value);
                }
#endif
            }
        }
    }

    protected virtual void OnDisable()
    {
        StopScan();
        //close serial port
        Close();
    }
    
    public void StartScan()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                //scan parameter
                var parameter = UtilitySCIP.CommandWriteShowParameter();
                serialPort.Write(parameter);
                Debug.Log(serialPort.ReadLine()); // ignore echo back

                var startMD = UtilitySCIP.CommandWriteMeasureDistance((int)rangeStep.x, (int)rangeStep.y, group, skip, 0, isShortRange);
                //Debug.Log(startMD);
                serialPort.Write(startMD);
                Debug.Log(serialPort.ReadLine()); // ignore echo back
            }
            catch (IOException ex)
            {
                Debug.LogErrorFormat("[IO Error]: {0}, {1}", ex.Source, ex.Message);
            }
        }
    }

    public void StopScan()
    {
        if (serialPort == null)
        {
            return;
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                //finish scanning
                var stopMD = UtilitySCIP.CommandWriteStopMeasure();
                serialPort.Write(stopMD);
                serialPort.ReadLine(); // ignore echo back
            }
            catch (IOException ex)
            {
                Debug.LogErrorFormat("[IO Error]: {0}, {1}", ex.Source, ex.Message);
            }
        }
    }

    /// <summary>
    /// Select COM port for serial communication
    /// </summary>
    /// <param name="portName">port to connect</param>
    public void ChangePort(string portName)
    {
        port = portName;
    }

    public bool Open()
    {
        if (serialPort == null)
        {
            return false;
        }

        if (!serialPort.IsOpen)
        {
            try
            {
                serialPort.Open();
                return true;
            }
            catch (IOException ex)
            {
                Debug.LogErrorFormat("[IO Error]: (Source){0}, (Message){1}", ex.Source, ex.Message);
            }
        }

        return false;
    }

    public void Close()
    {
        if (serialPort == null)
        {
            return;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                serialPort.Close();
            }
            catch (IOException ex)
            {
                Debug.LogErrorFormat("[IO Error]: (Source){0}, (Message){1}", ex.Source, ex.Message);
            }
        }
    }
}
