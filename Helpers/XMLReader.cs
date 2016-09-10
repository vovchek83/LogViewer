using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;

namespace LogViewer.Helpers
{
    public class XMLReader
    {

        public static void LoadXmlFile(string filePath , List<LogEntry> entry)
        {
            try
            {
                FileStream oFileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                StreamReader oStreamReader = new StreamReader(oFileStream);
                var sBuffer = string.Format("<root>{0}</root>", oStreamReader.ReadToEnd());
                oStreamReader.Close();
                oFileStream.Close();

                #region Read File Buffer

                ////////////////////////////////////////////////////////////////////////////////
                StringReader oStringReader = new StringReader(sBuffer);
                XmlTextReader oXmlTextReader = new XmlTextReader(oStringReader);
                oXmlTextReader.Namespaces = false;

                int iIndex = 1;
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);

                while (oXmlTextReader.Read())
                {
                    if ((oXmlTextReader.NodeType == XmlNodeType.Element) && (oXmlTextReader.Name == "log4j:event"))
                    {
                        LogEntry logentry = new LogEntry();

                        logentry.Item = iIndex;

                        double dSeconds = Convert.ToDouble(oXmlTextReader.GetAttribute("timestamp"));
                        logentry.TimeStamp = dt.AddMilliseconds(dSeconds).ToLocalTime();
                        logentry.Thread = oXmlTextReader.GetAttribute("thread");

                        #region get level

                        ////////////////////////////////////////////////////////////////////////////////
                        logentry.Level = oXmlTextReader.GetAttribute("level");
                        switch (logentry.Level)
                        {
                            case "ERROR":
                                {
                                    logentry.Image = LogEntry.Images(LogEntry.ImageType.Error);
                                    break;
                                }
                            case "INFO":
                                {
                                    logentry.Image = LogEntry.Images(LogEntry.ImageType.Info);
                                    break;
                                }
                            case "DEBUG":
                                {
                                    logentry.Image = LogEntry.Images(LogEntry.ImageType.Debug);
                                    break;
                                }
                            case "WARN":
                                {
                                    logentry.Image = LogEntry.Images(LogEntry.ImageType.Warn);
                                    break;
                                }
                            case "FATAL":
                                {
                                    logentry.Image = LogEntry.Images(LogEntry.ImageType.Fatal);
                                    break;
                                }
                            default:
                                {
                                    logentry.Image = LogEntry.Images(LogEntry.ImageType.Custom);
                                    break;
                                }
                        }
                        ////////////////////////////////////////////////////////////////////////////////

                        #endregion

                        #region read xml

                        ////////////////////////////////////////////////////////////////////////////////
                        while (oXmlTextReader.Read())
                        {
                            if (oXmlTextReader.Name == "log4j:event") // end element
                                break;
                            else
                            {
                                switch (oXmlTextReader.Name)
                                {
                                    case ("log4j:message"):
                                        {
                                            logentry.Message = oXmlTextReader.ReadString();
                                            break;
                                        }
                                    case ("log4j:data"):
                                        {
                                            switch (oXmlTextReader.GetAttribute("name"))
                                            {
                                                case ("log4jmachinename"):
                                                    {
                                                        logentry.MachineName = oXmlTextReader.GetAttribute("value");
                                                        break;
                                                    }
                                                case ("log4net:HostName"):
                                                    {
                                                        logentry.HostName = oXmlTextReader.GetAttribute("value");
                                                        break;
                                                    }
                                                case ("log4net:UserName"):
                                                    {
                                                        logentry.UserName = oXmlTextReader.GetAttribute("value");
                                                        break;
                                                    }
                                                case ("log4japp"):
                                                    {
                                                        logentry.App = oXmlTextReader.GetAttribute("value");
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                    case ("log4j:throwable"):
                                        {
                                            logentry.Throwable = oXmlTextReader.ReadString();
                                            break;
                                        }
                                    case ("log4j:locationInfo"):
                                        {
                                            logentry.Class = oXmlTextReader.GetAttribute("class");
                                            logentry.Method = oXmlTextReader.GetAttribute("method");
                                            logentry.File = oXmlTextReader.GetAttribute("file");
                                            logentry.Line = oXmlTextReader.GetAttribute("line");
                                            break;
                                        }
                                }
                            }
                        }
                        ////////////////////////////////////////////////////////////////////////////////

                        #endregion

                        entry.Add(logentry);
                        iIndex++;

                    }
                }
                ////////////////////////////////////////////////////////////////////////////////

                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}