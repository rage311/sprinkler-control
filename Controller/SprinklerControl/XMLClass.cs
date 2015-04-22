using System;
using System.Ext.Xml;
using System.Xml;
using System.Collections;
using System.IO;

using Microsoft.SPOT;

namespace SprinklerControl
{
    class XMLClass
    {
        public static void CreateXML(ArrayList XMLInfo, Stream stream)
        {
            try
            {
                //FileStream configFStream = FileSystem.GetFileStream("config.xml");
                //Stream stream = new Stream();
                XmlWriter xWriter = XmlWriter.Create(stream);

                //xWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                foreach (XMLPair pair in XMLInfo)
                {
                    switch (pair.XMLType)
                    {
                        case "StartElement":
                            xWriter.WriteStartElement(pair.varName);
                            break;
                        case "EndElement":
                            xWriter.WriteEndElement();
                            break;
                        case "String":
                            xWriter.WriteString(pair.varName);
                            break;
                        case "AttributeString":
                            xWriter.WriteAttributeString(pair.varName.Split('=')[0], pair.varName.Split('=')[1]);
                            break;
                        case "Raw":
                            xWriter.WriteRaw(pair.varName);
                            break;
                    }
                }

                                
            }
            catch (Exception e)
            {
                Debug.Print("CreateXML() exception: " + e.ToString());
                stream.Flush();
                stream.Close();
            }
            finally
            {
                //clean-up
                stream.Flush();
                //stream.Close();
            }
        }
    }
    
    public class XMLPair
    {
        public XMLPair(String argXMLType, String argVarName)
        {
            XMLType = argXMLType;
            varName = argVarName;
        }

        public String XMLType { get; set; }
        public String varName { get; set; }
    }
}
