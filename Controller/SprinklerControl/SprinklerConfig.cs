using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.IO;



namespace SprinklerControl
{
    enum SprinklerSchedule
    {
        EVERY_OTHER_DAY = 0,
        ODDS,
        EVENS,
        DAYS_OF_WEEK
    };


    public class SprinklerConfig
    {
        //Whether irrigation system is enabled
        public bool sysEnabled;

        //TODO: make arrays dynamic based on number of configured zones
        //Number of zones (sprinkler valves)
        public byte numZones;

        //Per zone sprinkler valve "on" durations
        public ArrayList durationMinutes;// = new ArrayList();// = new byte[numZones];                           

        //How many minutes each zone's start time is offset from system start time
        public short[] offsetMinutes;// = new ArrayList();// = new short[numZones];

        //System start time in minutes
        public short startTimeInMinutes;// = 53;

        public TimeSpan startTimeTOD = DateTime.Now.TimeOfDay;

        public byte daysOfWeek = 0;

        //System end time in minutes
        public short endTimeInMinutes;

        public OutputPort[] zoneValves;

        public byte scheduleType;

        public short totalRunTimeMinutes = 0;

        //public string configXMLString;

        private DateTime nextStartTime, nextEndTime;
        
        public byte[] configXML;

        short currentTimeInMinutes = 0;

        public bool runStatus = false;
        public sbyte zoneRunning = -1;
        public short minsUntilZoneStops = 0;

        bool demandRun = false;
        //0 for "demand run zone 1", 1 for zone 2, etc.        
        sbyte demandRunZone = -1,
              previousDemandRunZone = -1;
        

        //public XMLPair[] XMLParameters;
        
        //public String[,] XMLParameters = new String[2, NUM_OF_XML_PAIRS];

        public ArrayList XMLParameters;


        public SprinklerConfig()
        {
            //XMLParameters = new Hashtable();
            XMLParameters = new ArrayList();

            scheduleType = (byte)SprinklerSchedule.ODDS;

            durationMinutes = new ArrayList();

            if (FileSystem.MountSDCard())
            {
                FileStream configFStream = FileSystem.GetFileStream("config.xml");
                if (configFStream != null)
                {
                    configXML = new byte[(int)configFStream.Length];
                    configFStream.Read(configXML, 0, (int)configFStream.Length);
                    //configXMLString = new string(Encoding.UTF8.GetChars(fStreamBuffer));
                    
                    configFStream.Close();

                    //XmlReader xReader = XmlReader.Create(FileSystem.GetFileStream("config.xml"));
                    SetClassProperties(XmlReader.Create(FileSystem.GetFileStream("config.xml")));
                    

                    // TODO: Assign valves to pins and/or iterate through pins (only assign starting pin)
                    zoneValves = new OutputPort[numZones];
                    zoneValves[0] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di20, true);
                    zoneValves[1] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di21, true);
                    zoneValves[2] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di22, true);
                    zoneValves[3] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di23, true);
                    zoneValves[4] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di24, true);
                    zoneValves[5] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di25, true);
                    zoneValves[6] = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di26, true);                    
                }
                else
                    Debug.Print("SprinklerConfig() failed.  FileStream couldn't be opened.");
            }
            else
                Debug.Print("SprinklerConfig() failed.  SD card couldn't be mounted.");
        }


        public void SetClassProperties(XmlReader xReader)
        {
            if (xReader != null)
            {                
                short tempStartTimeInMinutes = 0;

                while (!xReader.EOF)
                {
                    xReader.Read();
                    switch (xReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            Debug.Print("element: " + xReader.Name);

                            if (xReader.Name != "SprinklerConfig")
                            {
                                //Debug.Print("ReadElementString: " + xReader.ReadElementString());
                                //Debug.Print("ReadString " + xReader.ReadString());
                                //String elementString = xReader.ReadElementString();
                                switch (xReader.Name)
                                {
                                    case "ZoneDuration":
                                        byte id = Convert.ToByte(xReader.GetAttribute("id"));
                                        Debug.Print("id = " + id);

                                        String elementString = xReader.ReadElementString();
                                        
                                        if (id < durationMinutes.Count)
                                            durationMinutes[id] = Convert.ToInt16(elementString);
                                        else
                                            durationMinutes.Add(Convert.ToInt16(elementString));

                                        Debug.Print("duration: " + durationMinutes[id]);

                                        break;
                                    case "StartTimeHours":
                                        tempStartTimeInMinutes += (short)(Convert.ToInt16(xReader.ReadElementString()) * 60);
                                        break;
                                    case "StartTimeMinutes":
                                        tempStartTimeInMinutes += Convert.ToInt16(xReader.ReadElementString());
                                        break;
                                    case "SysEnabled":
                                        sysEnabled = xReader.ReadElementString() == "1";
                                        break;
                                    case "Schedule":
                                        switch (xReader.ReadElementString())
                                        {
                                            case "daysofweek":
                                                scheduleType = (byte)SprinklerSchedule.DAYS_OF_WEEK;
                                                break;
                                            case "evens":
                                                scheduleType = (byte)SprinklerSchedule.EVENS;
                                                break;
                                            case "odds":
                                            default:
                                                scheduleType = (byte)SprinklerSchedule.ODDS;
                                                break;
                                        }
                                        break;
                                    case "DaysOfWeek":
                                        daysOfWeek = Convert.ToByte(xReader.ReadElementString());
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;

                        default:
                            //Debug.Print(xReader.NodeType);
                            break;
                    }
                }

                if (tempStartTimeInMinutes > 0)
                    startTimeInMinutes = tempStartTimeInMinutes;

                SetCalculatedProperties();
            }
        }


        public void SetCalculatedProperties()
        {            
            // setting calculated properties
            Debug.Print("Zone count: " + durationMinutes.Count);
            numZones = (byte)durationMinutes.Count;

            offsetMinutes = new short[numZones];

            endTimeInMinutes = startTimeInMinutes;
            for (byte i = 0; i < numZones; i++)
            {
                totalRunTimeMinutes += (short)durationMinutes[i];
                endTimeInMinutes += (short)durationMinutes[i];
                offsetMinutes[i] = (short)(endTimeInMinutes - startTimeInMinutes);
            }

            startTimeTOD = new TimeSpan((int)(startTimeInMinutes/60), (int)(startTimeInMinutes % 60), 0);

            CalculateNextRunPeriod();

            Debug.Print(endTimeInMinutes.ToString());
        }


        public void WritePin(OutputPort port, bool onOff)
        {
            port.Write(!onOff);
        }

        public bool ReadPin(OutputPort port)
        {
            return !port.Read();
        }

        public void TurnAllZonesOff()
        {
            foreach (OutputPort port in zoneValves)
                WritePin(port, false);
        }
        

        public void CheckRunCriteria(object data)
        {
            if (demandRun)
            {
                if (demandRunZone > -1 && demandRunZone != previousDemandRunZone)
                {
                    Debug.Print("demandRunZone > -1 && demandRunZone != previousDemandRunZone");

                    for (int i = 0; i < zoneValves.Length; i++)
                    {
                        if (i != demandRunZone)
                            WritePin(zoneValves[i], false);
                    }

                    WritePin(zoneValves[demandRunZone], true);

                    previousDemandRunZone = demandRunZone;
                    runStatus = true;
                }
                else if ((demandRunZone == -1 && previousDemandRunZone > -1) ||
                         (demandRunZone > -1 && demandRunZone == previousDemandRunZone))
                {
                    Debug.Print("demandRunZone == -1 && previousDemandRunZone > -1");

                    TurnAllZonesOff();

                    runStatus = false;
                    demandRunZone = -1;
                    previousDemandRunZone = -1;
                }
                demandRun = false;
            }
            else if (sysEnabled && demandRunZone == -1)
            {
                Debug.Print("sysEnabled");
                Debug.Print("RealTimeClock: " + RealTimeClock.GetTime().Hour + ":" + RealTimeClock.GetTime().Minute + ":" + RealTimeClock.GetTime().Second);
                Debug.Print("nextStartTime: " + nextStartTime);

                if (RealTimeClock.GetTime() >= nextStartTime && RealTimeClock.GetTime() < nextEndTime)
                {
                    bool found = false;
                    byte i = 1;
                    do
                    {
                        // test to see if zone[i] should be running
                        if (nextStartTime.AddMinutes(offsetMinutes[i - 1]) > RealTimeClock.GetTime())
                        {
                            found = true;
                            minsUntilZoneStops = (short)(startTimeInMinutes + offsetMinutes[i - 1] - currentTimeInMinutes);
                            minsUntilZoneStops = (short)((nextStartTime.AddMinutes(offsetMinutes[i - 1]).Subtract(RealTimeClock.GetTime()).Hours * 60) +
                                                         (nextStartTime.AddMinutes(offsetMinutes[i - 1]).Subtract(RealTimeClock.GetTime()).Minutes));

                            if (!ReadPin(zoneValves[i - 1]))
                            {
                                foreach (OutputPort port in zoneValves)
                                    WritePin(port, false);

                                runStatus = true;
                                WritePin(zoneValves[i - 1], true);
                            }
                        }
                        else
                            i++;
                    } while (found == false);
                }
                else if (RealTimeClock.GetTime() >= nextEndTime)
                {
                    runStatus = false;
                    WritePin(zoneValves[zoneValves.Length - 1], false);

                    CalculateNextRunPeriod();
                }
            }            
        }


        // TODO: finish stub...
        // to be called when controller starts up and at the end of each run period
        public void CalculateNextRunPeriod()
        {
            switch (scheduleType)
            {
                case (int)SprinklerSchedule.ODDS:
                    // normalize either the odd or the even day
                    // back to the beginning of the current/last odd day
                    DateTime normalizedOddDate = RealTimeClock.GetTime().Date.Add(new TimeSpan((RealTimeClock.GetTime().Day % 2) - 1, 0, 0, 0));                    
                    
                    // if current time is before the end of the next/current run                    
                    if (RealTimeClock.GetTime() < normalizedOddDate.Date.Add(startTimeTOD).AddMinutes(totalRunTimeMinutes))
                        nextStartTime = normalizedOddDate.Add(startTimeTOD);
                    else
                        nextStartTime = normalizedOddDate.AddDays(2 - (normalizedOddDate.AddDays(1).Day % 2)).Add(startTimeTOD);

                    nextEndTime = nextStartTime.AddMinutes(totalRunTimeMinutes);
                    
                    break;
                case (int)SprinklerSchedule.EVENS:
                    // normalize either the odd or the even day
                    // back to the beginning of the current/last odd day
                    DateTime normalizedEvenDate = RealTimeClock.GetTime().Date.Add(new TimeSpan((RealTimeClock.GetTime().Day % 2), 0, 0, 0));

                    // if current time is before the end of the next/current run                    
                    if (RealTimeClock.GetTime() < normalizedEvenDate.Date.Add(startTimeTOD).AddMinutes(totalRunTimeMinutes))
                        nextStartTime = normalizedEvenDate.Add(startTimeTOD);
                    else
                        //two days from the normalized even day is not even
                        if (normalizedEvenDate.AddDays(2).Day %2 != 0)
                            nextStartTime = normalizedEvenDate.AddDays(3).Add(startTimeTOD);
                        else
                            nextStartTime = normalizedEvenDate.AddDays(2).Add(startTimeTOD);

                    nextEndTime = nextStartTime.AddMinutes(totalRunTimeMinutes);

                    break;

                case (int)SprinklerSchedule.DAYS_OF_WEEK:
                    //check to see if any days of the week are scheduled
                    if (daysOfWeek != 0)
                    {
                        if ((((1 << (int)RealTimeClock.GetTime().DayOfWeek) & daysOfWeek) > 0) &&
                            RealTimeClock.GetTime() < RealTimeClock.GetTime().Date.Add(startTimeTOD).AddMinutes(totalRunTimeMinutes))
                            nextStartTime = RealTimeClock.GetTime().Date.Add(startTimeTOD);
                        else
                        {
                            int iDayOfWeek = (int)RealTimeClock.GetTime().AddDays(1).DayOfWeek;
                            int iAddedDays = 1;
                            while (iAddedDays < 8 && RealTimeClock.GetTime() > nextStartTime)
                            {
                                //iDayOfWeek isn't a scheduled day
                                if (((1 << iDayOfWeek) & daysOfWeek) < 1)
                                {
                                    if (iDayOfWeek < 6)
                                        iDayOfWeek++;
                                    else
                                        iDayOfWeek = 0;

                                    iAddedDays++;
                                }
                                else
                                {
                                    if (iAddedDays == 0)
                                        iAddedDays = 7;

                                    nextStartTime = RealTimeClock.GetTime().Date.AddDays(iAddedDays).Add(startTimeTOD);
                                    nextEndTime = nextStartTime.AddMinutes(totalRunTimeMinutes);
                                }
                            }
                        }
                    }
                    else
                        nextStartTime = new DateTime(1970, 1, 1);
                    
                    break;
            }
        }
        

        // TODO: stub?
        public void SetParams(string parameters)
        {
            string[] splitParams = parameters.Split('&');
            foreach (string param in splitParams)
            {
                string[] paramSet = param.Split('=');
                switch (paramSet[0])
                {
                    case "zone":
                        
                        return;
                }  

            }
        }


        public void ReadXMLIntoString()
        {
            CreateXMLParameters();
        }


        public void WriteXMLToFile()
        {
            FileStream fileStream = FileSystem.GetFileStream("config.xml");
            if (fileStream != null)
            {
                
                fileStream.Flush();
                fileStream.Close();
            }
        }

        //Returns a byte array of the XMLParameters
        public byte[] ReturnValuesAsXML()
        {
            MemoryStream stream = new MemoryStream();
            CreateXMLParameters();
            XMLClass.CreateXML(XMLParameters, stream);
            //Array.Clear(configXML, 0, configXML.Length);
            configXML = null;
            //configXML = new byte[stream.Length];
            configXML = stream.ToArray();
            //stream.Read(configXML, 0, (int)stream.Length);
            stream.Close();

            return configXML;
        }

        //Populates XMLParameters
        public void CreateXMLParameters()
        {            
            XMLParameters.Clear();

            // generate XML describing the current status of the sprinklers
            XMLParameters.Add(new XMLPair("StartElement", "SprinklerStatus"));

                XMLParameters.Add(new XMLPair("StartElement", "ScheduleType"));
                XMLParameters.Add(new XMLPair("String", scheduleType.ToString()));
                XMLParameters.Add(new XMLPair("EndElement", "ScheduleType"));

                XMLParameters.Add(new XMLPair("StartElement", "RunStatus"));
                    XMLParameters.Add(new XMLPair("String", runStatus.ToString()));
                XMLParameters.Add(new XMLPair("EndElement", "RunStatus"));
                
                XMLParameters.Add(new XMLPair("StartElement", "SysEnabled"));
                XMLParameters.Add(new XMLPair("String", sysEnabled.ToString()));
                XMLParameters.Add(new XMLPair("EndElement", "SysEnabled"));

                for (int i = 0; i < durationMinutes.Count; i++)
                {
                    XMLParameters.Add(new XMLPair("StartElement", "ZoneRuntimeRemaining"));
                    XMLParameters.Add(new XMLPair("AttributeString", "id=" + i));
                    if (ReadPin(zoneValves[i]))
                        XMLParameters.Add(new XMLPair("String", minsUntilZoneStops.ToString()));
                    else
                        XMLParameters.Add(new XMLPair("String", "0"));
                    XMLParameters.Add(new XMLPair("EndElement", "ZoneRuntimeRemaining"));
                    //XMLParameters.Add("Raw", "\r\n\t");
                }
            XMLParameters.Add(new XMLPair("EndElement", "SprinklerStatus"));

            

            // generate XML describing the current configuration of the sprinklers
            XMLParameters.Add(new XMLPair("StartElement", "SprinklerConfig"));
            //XMLParameters.Add("Raw", "\r\n\t");

                for (int i = 0; i < durationMinutes.Count; i++)
                {
                    XMLParameters.Add(new XMLPair("StartElement", "ZoneDuration"));
                    XMLParameters.Add(new XMLPair("AttributeString", "id=" + i));
                    XMLParameters.Add(new XMLPair("String", durationMinutes[i].ToString()));
                    XMLParameters.Add(new XMLPair("EndElement", "ZoneDuration"));
                    //XMLParameters.Add("Raw", "\r\n\t");
                }

                XMLParameters.Add(new XMLPair("StartElement", "StartTime"));
                XMLParameters.Add(new XMLPair("String", startTimeInMinutes.ToString()));
                XMLParameters.Add(new XMLPair("EndElement", "StartTime"));
                //XMLParameters.Add("Raw", "\r\n\t");

                XMLParameters.Add(new XMLPair("StartElement", "SysEnabled"));
                XMLParameters.Add(new XMLPair("String", sysEnabled.ToString()));
                XMLParameters.Add(new XMLPair("EndElement", "SysEnabled"));
                //XMLParameters.Add("Raw", "\r\n");
            
                XMLParameters.Add(new XMLPair("StartElement", "DaysOfWeek"));
                XMLParameters.Add(new XMLPair("String", daysOfWeek.ToString()));
                XMLParameters.Add(new XMLPair("EndElement", "DaysOfWeek"));

            XMLParameters.Add(new XMLPair("EndElement", "SprinklerConfig"));

            //return XMLParameters;
            //XMLClass.CreateXML(XMLParameters, fileStream);
        }
        
/* DEPRECATED
        public string serializeParameters()
        {


            string returnXML = "<SprinklerConfig>";
            
            for (int i = 0; i < Program.sprinklerConfig.numZones; i++)
                returnXML += "\r\n\t<ZoneDuration id=\"" + i + "\">" + Program.sprinklerConfig.durationMinutes[i] + "</ZoneDuration>";
            
            returnXML += "\r\n\t<StartTime>" + Program.sprinklerConfig.startTimeInMinutes + "</StartTime>" +
                         "\r\n\t<SysEnabled>" + Program.sprinklerConfig.sysEnabled.ToString() + "</SysEnabled>" +
                         "\r\n</SprinklerConfig>";

            return returnXML;
        }
*/

        public void setDemand(sbyte demandZone)
        {
            demandRunZone = demandZone;
            demandRun = true;
            CheckRunCriteria(null);
        }
    }
}