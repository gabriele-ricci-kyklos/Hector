using Hector.Core.Support;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Hector.Core.Serialization.Xml
{
    public static class QuickXmlSerializer
    {
        public static string SerializeObject<T>(T o)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter tw = null;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                tw = new XmlTextWriter(sw);
                serializer.Serialize(tw, o);
            }
            finally
            {
                sw.Close();
                if (tw.IsNotNull())
                {
                    tw.Close();
                }
            }

            return sw.ToString();
        }

        public static T DeserializeObject<T>(string xml)
        {
            xml.AssertHasText("xml");

            StringReader strReader = null;
            XmlSerializer serializer = null;
            XmlTextReader xmlReader = null;
            T returnObj = default(T);

            try
            {
                object obj = null;
                strReader = new StringReader(xml);
                serializer = new XmlSerializer(typeof(T));
                xmlReader = new XmlTextReader(strReader);
                obj = serializer.Deserialize(xmlReader);

                if (obj is T)
                {
                    returnObj = (T)obj;
                }
                else
                {
                    returnObj = (T)Convert.ChangeType(obj, typeof(T));
                }
            }
            catch (InvalidCastException)
            {
                return returnObj;
            }
            finally
            {
                if (xmlReader.IsNotNull())
                {
                    xmlReader.Close();
                }
                if (strReader.IsNotNull())
                {
                    strReader.Close();
                }
            }

            return returnObj;
        }
    }
}
