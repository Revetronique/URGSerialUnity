using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using UnityEngine;

public static class SerialPortExpansion
{
    public static Task<string> ReadLineAsync(this SerialPort port, string encode = "utf-8")
    {
        return Task.Run(async () =>
        {
            try
            {
                var buffer = new byte[port.BytesToRead];
                var size = await port.BaseStream.ReadAsync(buffer, 0, buffer.Length);

                return buffer;
            }
            catch (IOException ex)
            {
                Debug.LogErrorFormat("[IO Error]: (Source){0}, (Message){1}", ex.Source, ex.Message);
                return null;
            }
        }).ContinueWith((t) =>
        {
            if (t.IsCompleted)
            {
                var str = System.Text.Encoding.GetEncoding(encode).GetString(t.Result);
                return str;
                //the first captured texts splitted with LF
                //return str.Substring(0, str.IndexOf(port.NewLine));
            }
            else
            {
                return string.Empty;
            }
        });
    }
}

namespace URG
{
    public class UtilitySCIP
    {
        //[TODO]: check sum process

        public static readonly string[] HeaderTimeStamp = { "TM0", "TM1", "TM2" };
        public static readonly string HeaderChangeBPS = "SS";
        public static readonly string HeaderTurnOnLaser = "BM";
        public static readonly string HeaderTurnOffLaser = "QT";
        public static readonly string HeaderResetStatus = "RS";
        public static readonly string HeaderChangeMotorSpeed = "CR";
        public static readonly string HeaderChangeMeasureMode = "HS";
        public static readonly string HeaderShowVersion = "VV";
        public static readonly string HeaderShowParameter = "PP";
        public static readonly string HeaderShowStatus = "II";
        public static readonly string HeaderDebugTest = "DB";
        public static readonly string HeaderMeasureDistance = "M";
        public static readonly string HeaderGrabDistance = "G";
        public static readonly string HeaderGet3Bytes = "D";
        public static readonly string HeaderGet2Bytes = "S";

        public static readonly int StepMin = 44;    //0
        public static readonly int StepMax = 725;   //768
        static readonly int stepCenter = (StepMin + StepMax) / 2;
        static readonly float resAngle = 360 / 1024.0f;

        /// <summary>
        /// delegate to check receive packets
        /// </summary>
        /// <param name="packet">Packets sent from LRF device</param>
        /// <returns>whether packets have correct data for process</returns>
        public delegate bool CheckPacket(string[] packet);

        public enum DebugCode
        {
            SCIP11 = 1,
            SCIP20 = 2,
            RECOVER = 3,    //normal -> error -> normal
            BROKEN = 4,     //normal -> error -> broken
            CRASH = 5,      //normal -> broken
            RESTORE = 10,   //exit debug mode
        }

        #region ReadMethod
        public static Dictionary<float, long> ProcessReadDistance(string getCommand, bool realtime = true, bool shortRange = false, string message = "")
        {
            var points = new Dictionary<float, long>();

            var header = (realtime ? HeaderMeasureDistance : HeaderGrabDistance)
                        + (shortRange ? HeaderGet2Bytes : HeaderGet3Bytes);

            var command = processReadCommon(getCommand, header, message, checkPacketMeasure);
            if (!string.IsNullOrEmpty(command[0]))
            {
                //time stamp
                long timeStamp = decode(command[1], 4);
                //scanning parameter
                int start = int.Parse(command[0].Substring(2, 4));
                int end = int.Parse(command[0].Substring(6, 4));
                int group = int.Parse(command[0].Substring(10, 2));

                //set the distance value in each angle
                int size = shortRange ? 2 : 3;
                points = getPointDistance(command.GetRange(2, command.Count - 2), size, start, end, group);
            }

            return points;
        }

        /// <summary>
        /// get distance data of scanned points from Measure("MD" or "MS") command
        /// </summary>
        /// <param name="getCommand">receive packet</param>
        /// <param name="points">distance in each point (angle)</param>
        /// <param name="timeStamp">time stamp of scanning</param>
        /// <param name="realtime">scan in realtime?</param>
        /// <param name="shortRange">use short range?</param>
        /// <param name="message">arbitrary message</param>
        /// <returns>success or not</returns>
        public static bool ProcessReadDistance(string getCommand, out Dictionary<float, long> points, ref long timeStamp, bool realtime = true, bool shortRange = false, string message = "")
        {
            points = new Dictionary<float, long>();

            var header = (realtime ? HeaderMeasureDistance : HeaderGrabDistance)
                        + (shortRange ? HeaderGet2Bytes : HeaderGet3Bytes);

            var command = processReadCommon(getCommand, header, message, checkPacketMeasure);
            if (command?.Count > 0)
            {
                //time stamp
                timeStamp = decode(command[1], 4);
                //scanning parameter
                int start = int.Parse(command[0].Substring(2, 4));
                int end = int.Parse(command[0].Substring(6, 4));
                int group = int.Parse(command[0].Substring(10, 2));

                //set the distance value in each angle
                int size = shortRange ? 2 : 3;
                points = getPointDistance(command.GetRange(2, command.Count - 2), size, start, end, group);

                return true;
            }

            return false;
        }

        /// <summary>
        /// decode data to seize the measured distance
        /// </summary>
        /// <param name="lines">list of string data containing distance data</param>
        /// <param name="size">byte size of the distance in each point</param>
        /// <param name="start">start step of the scaning point</param>
        /// <param name="end">end step of the scaning point</param>
        /// <param name="grouping">step interval</param>
        /// <returns>distance at each angle (center as 0 deg.)</returns>
        public static Dictionary<float, long> getPointDistance(List<string> lines, int size, int start, int end, int group)
        {
            //measured distance at each angle
            var points = new Dictionary<float, long>();

            //summarize all elements of the text in the array of distance data into one sequence
            System.Text.StringBuilder sb = new System.Text.StringBuilder(lines.Count);
            foreach (var line in lines)
            {
                sb.Append(line.Substring(0, line.Length - 1));
            }
            //set the distance value in each angle
            for (int i = 0; i <= (end - start) / group; i++)
            {
                var angle = (start + group * i - stepCenter) * resAngle;
                var distance = decode(sb.ToString(), size, i * size);

                points.Add(angle, distance);
            }

            return points;
        }

        /// <summary>
        /// Check process to receive MD command message successfully
        /// </summary>
        /// <param name="command">receive packet</param>
        /// <param name="result">data for latter processes</param>
        /// <returns>success or not</returns>
        static bool checkPacketMeasure(string[] command)
        {
            // whether the error code indicates success or not
            return command[1].StartsWith("99") || command[1].StartsWith("00");
        }

        /// <summary>
        /// Common reading process (check header and message)
        /// </summary>
        /// <param name="getCommand">received raw message</param>
        /// <param name="header">header of the packet</param>
        /// <param name="message">user's arbitrary message</param>
        /// <param name="result">data for necessary process</param>
        /// <param name="check">packet check and store the necessary data in result</param>
        /// <returns>is the process succeeded (a single empty value if the process fails)?</returns>
        static List<string> processReadCommon(string getCommand, string header, string message, CheckPacket check)
        {
            var command = getCommand.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries) ?? new string[] { string.Empty };

            //the 1st element doesn't match the header
            if (!command[0].StartsWith(header))
            {
                return new List<string>();
            }
            //only the case that sending a measurement command and receive the status code 99,
            else if (!string.IsNullOrEmpty(message) && !command[0].EndsWith(message))
            {
                return new List<string>();
            }
            //not satisfied any other conditions defined in a delegate method "check"
            else if (!check(command))
            {
                return new List<string>();
            }

            var result = new List<string>();
            //store parameters of the measure process
            result.AddRange(command);
            //remove header
            result.RemoveAt(1);

            return result;
        }
        #endregion

        #region WriteCommand
        /// <summary>
        /// Create Measurement command
        /// </summary>
        /// <param name="start">measurement start step</param>
        /// <param name="end">measurement end step</param>
        /// <param name="grouping">grouping step number</param>
        /// <param name="skips">skip scan number</param>
        /// <param name="scans">get scan numbar</param>
        /// <param name="isRangeShort">use short range mode</param>
        /// <param name="input">text contained the message</param>
        /// <returns>created command</returns>
        public static string CommandWriteMeasureDistance(int start, int end, int grouping = 1, int skips = 0, int scans = 0, bool isRangeShort = false, string input = "")
        {
            string header = HeaderMeasureDistance;
            //get short range distance or normal
            header += isRangeShort ? HeaderGet2Bytes : HeaderGet3Bytes;

            return header + start.ToString("D4") + end.ToString("D4") + grouping.ToString("D2") + skips.ToString("D1") + scans.ToString("D2") + GenerateContext(input) + "\n";
        }

        public static string CommandWriteGrapDistance(int start, int end, int grouping = 1, bool isRangeShort = false, string input = "")
        {
            string header = HeaderGrabDistance;
            //get short range distance or normal
            header += isRangeShort ? HeaderGet2Bytes : HeaderGet3Bytes;

            return header + start.ToString("D4") + end.ToString("D4") + grouping.ToString("D2") + GenerateContext(input) + "\n";
        }

        public static string CommandWriteStartMeasure(string input = "")
        {
            return HeaderTurnOnLaser + GenerateContext(input) + "\n";
        }

        public static string CommandWriteStopMeasure(string input = "")
        {
            return HeaderTurnOffLaser + GenerateContext(input) + "\n";
        }

        public static string CommandWriteResetSensor(string input = "")
        {
            return HeaderResetStatus + GenerateContext(input) + "\n";
        }

        public static string CommandWriteShowVersion(string input = "")
        {
            return HeaderShowVersion + GenerateContext(input) + "\n";
        }

        public static string CommandWriteShowParameter(string input = "")
        {
            return HeaderShowParameter + GenerateContext(input) + "\n";
        }

        public static string CommandWriteShowStatus(string input = "")
        {
            return HeaderShowStatus + GenerateContext(input) + "\n";
        }

        public static string CommandWriteSetTime(uint code = 0, string input = "")
        {
            if (code < HeaderTimeStamp.Length)
            {
                return HeaderTimeStamp[code] + GenerateContext(input) + "\n";
            }
            return string.Empty;
        }

        public static string CommandWriteDebutTest(string code = "02", string input = "")
        {
            return HeaderDebugTest + code + GenerateContext(input) + "\n";
        }

        public static string CommandWriteSetBaudRate(uint baudrate = 115200, string input = "")
        {
            //string of the baudrate needs to have 6 characters
            return HeaderChangeBPS + baudrate.ToString("D6") + GenerateContext(input) + "\n";
        }

        public static string CommandWriteSetMotorSpeed(string rate = "00", string input = "")
        {
            return HeaderChangeMotorSpeed + rate + GenerateContext(input) + "\n";
        }

        public static string CommandWriteSetMeasureMode(uint mode = 0, string input = "")
        {
            return HeaderChangeMeasureMode + mode + GenerateContext(input) + "\n";
        }
        #endregion

        #region Utility
        /// <summary>
        /// Create arbitrary message containd to the sent message.
        /// Use alphabets, number, underbar, atmark, plus, minus, or dot only. 16 or less characters.
        /// </summary>
        /// <param name="input">your arbitrary text</param>
        /// <returns>formatted text</returns>
        public static string GenerateContext(string input)
        {
            //extract the one which has the maximum number of characters to match
            //\w: alphabets, number, underbar
            //@: at mark
            //\ -> escape sequence (., +, and - is special characters)
            //{m, n}: repeat m to n
            var regex = new System.Text.RegularExpressions.Regex(@"[\w@\.\+\-]{1,16}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            //find a text matching to the searching terms
            if (regex.IsMatch(input))
            {
                var match = regex.Match(input);
                //add delimiter ";" on the top of extracted text
                return ";" + match.Value;
            }
            //if the inputed text is not matched the term, return empty string
            return string.Empty;
        }

        /// <summary>
        /// decode part of string 
        /// </summary>
        /// <param name="data">encoded string</param>
        /// <param name="size">encode size</param>
        /// <param name="offset">decode start position</param>
        /// <returns>decode result</returns>
        static long decode(string data, int size, int offset = 0)
        {
            long value = 0;

            for (int i = 0; i < size; ++i)
            {
                value <<= 6;
                value |= (long)data[offset + i] - 0x30;
            }

            return value;
        }
        #endregion
    }
}
