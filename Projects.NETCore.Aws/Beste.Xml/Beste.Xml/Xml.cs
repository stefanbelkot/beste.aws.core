using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Beste.Xml
{
    public abstract class Xml : IXml
    {
        
        private static Dictionary<Type, XmlSerializer> serializers = new Dictionary<Type, XmlSerializer>();
        private static XmlSerializer Serializer<T>()
        {
            Type type = typeof(T);
            if (!serializers.ContainsKey(type))
            {
                serializers.Add(type, new XmlSerializer(type));
            }
            return serializers[type];
        }
        private static XmlSerializer Serializer(Type type)
        {
            if (!serializers.ContainsKey(type))
            {
                serializers.Add(type, new XmlSerializer(type));
            }
            return serializers[type];
        }

        /// <summary>
        /// Serializes object into an XML document
        /// </summary>
        /// <param name="encoding">encoding of the xml</param>
        /// <returns>string XML value</returns>
        public virtual string Serialize(System.Text.Encoding encoding)
        {
            System.IO.StreamReader streamReader = null;
            System.IO.MemoryStream memoryStream = null;
            try
            {
                memoryStream = new System.IO.MemoryStream();
                System.Xml.XmlWriterSettings xmlWriterSettings = new System.Xml.XmlWriterSettings();
                xmlWriterSettings.NewLineChars = Environment.NewLine;
                xmlWriterSettings.NewLineOnAttributes = true;
                xmlWriterSettings.Indent = true;
                xmlWriterSettings.Encoding = encoding;
                System.Xml.XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);
                Serializer(GetType()).Serialize(xmlWriter, this);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                streamReader = new System.IO.StreamReader(memoryStream);
                return streamReader.ReadToEnd();
            }
            finally
            {
                if ((streamReader != null))
                {
                    streamReader.Dispose();
                }
                if ((memoryStream != null))
                {
                    memoryStream.Dispose();
                }
            }
        }


        /// <summary>
        /// Serializes object into an XML document with default encoding UTF8
        /// </summary>
        /// <returns>string XML value</returns>
        public virtual string Serialize()
        {
            return Serialize(Encoding.UTF8);
        }

        /// <summary>
        /// Deserializes a xmlString in an object of this class
        /// </summary>
        /// <param name="xml">string to deserialize</param>
        /// <param name="obj">Output object</param>
        /// <param name="exception">output Exception value if deserialize failed</param>
        /// <returns>true if this XmlSerializer can deserialize the object; otherwise, false</returns>
        public static bool Deserialize<T>(string xml, out T obj, out System.Exception exception)
        {
            exception = null;
            obj = default(T);
            try
            {
                obj = Deserialize<T>(xml);
                return true;
            }
            catch (System.Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary>
        /// Deserializes a xmlString in an object of this class
        /// </summary>
        /// <param name="xml">string to deserialize</param>
        /// <param name="obj">Output object</param>
        /// <returns>true if this XmlSerializer can deserialize the object; otherwise, false</returns>
        public static bool Deserialize<T>(string xml, out T obj)
        {
            System.Exception exception = null;
            return Deserialize<T>(xml, out obj, out exception);
        }

        /// <summary>
        /// Deserializes a xmlString in an object of this class
        /// </summary>
        /// <typeparam name="T">type to deserialize into</typeparam>
        /// <param name="xml">string to deserialize</param>
        /// <returns>an object of type T</returns>
        public static T Deserialize<T>(string xml)
        {
            System.IO.StringReader stringReader = null;
            try
            {
                stringReader = new System.IO.StringReader(xml);
                return ((T)(Serializer<T>().Deserialize(System.Xml.XmlReader.Create(stringReader))));
            }
            finally
            {
                if ((stringReader != null))
                {
                    stringReader.Dispose();
                }
            }
        }

        /// <summary>
        /// Serializes the object into file
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        /// <param name="encoding">encoding of the file</param>
        /// <param name="exception">output Exception value if failed</param>
        /// <returns>true if can serialize and save into file; otherwise, false</returns>
        public virtual bool SaveToFile(string fileName, System.Text.Encoding encoding, out System.Exception exception)
        {
            exception = null;
            try
            {
                SaveToFile(fileName, encoding);
                return true;
            }
            catch (System.Exception e)
            {
                exception = e;
                return false;
            }
        }

        /// <summary>
        /// Serializes the object into file with default encoding UTF8
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        /// <param name="exception">output Exception value if failed</param>
        /// <returns>true if can serialize and save into file; otherwise, false</returns>
        public virtual bool SaveToFile(string fileName, out System.Exception exception)
        {
            return SaveToFile(fileName, Encoding.UTF8, out exception);
        }

        /// <summary>
        /// Serializes the object into file with default encoding UTF8
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        public virtual void SaveToFile(string fileName)
        {
            SaveToFile(fileName, Encoding.UTF8);
        }

        /// <summary>
        /// Serializes the object into file
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        /// <param name="encoding">encoding of the file</param>
        public virtual void SaveToFile(string fileName, System.Text.Encoding encoding)
        {
            System.IO.StreamWriter streamWriter = null;
            try
            {
                string xmlString = Serialize(encoding);
                streamWriter = new System.IO.StreamWriter(fileName, false, Encoding.UTF8);
                streamWriter.WriteLine(xmlString);
                streamWriter.Close();
            }
            finally
            {
                if ((streamWriter != null))
                {
                    streamWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// Deserializes xml from file into an object of this class
        /// </summary>
        /// <param name="fileName">string xml file to load and deserialize</param>
        /// <param name="encoding">encoding of the file</param>
        /// <param name="obj">Output object</param>
        /// <param name="exception">output Exception value if deserialize failed</param>
        /// <returns>true if this XmlSerializer can deserialize the object; otherwise, false</returns>
        public static bool LoadFromFile<T>(string fileName, System.Text.Encoding encoding, out T obj, out System.Exception exception)
        {
            exception = null;
            obj = default(T);
            try
            {
                obj = LoadFromFile<T>(fileName, encoding);
                return true;
            }
            catch (System.Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary>
        /// Deserializes xml from file with default encoding UTF8 into an object of this class 
        /// </summary>
        /// <param name="fileName">string xml file to load and deserialize</param>
        /// <param name="obj">Output object</param>
        /// <param name="exception">output Exception value if deserialize failed</param>
        /// <returns>true if this XmlSerializer can deserialize the object; otherwise, false</returns>
        public static bool LoadFromFile<T>(string fileName, out T obj, out System.Exception exception)
        {
            return LoadFromFile<T>(fileName, Encoding.UTF8, out obj, out exception);
        }

        /// <summary>
        /// Deserializes xml from file with default encoding UTF8 into an object of this class
        /// </summary>
        /// <param name="fileName">string xml file to load and deserialize</param>
        /// <param name="obj">Output object</param>
        /// <returns>true if this XmlSerializer can deserialize the object; otherwise, false</returns>
        public static bool LoadFromFile<T>(string fileName, out T obj)
        {
            System.Exception exception = null;
            return LoadFromFile<T>(fileName, out obj, out exception);
        }

        /// <summary>
        /// Deserializes xml from file with default encoding UTF8 into an object of this class
        /// </summary>
        /// <param name="fileName">string xml file to load and deserialize</param>
        /// <returns>an object of type T</returns>
        public static T LoadFromFile<T>(string fileName)
        {
            return LoadFromFile<T>(fileName, Encoding.UTF8);
        }

        /// <summary>
        /// Deserializes xml from file into an object of this class
        /// </summary>
        /// <param name="filePath">string xml file to load and deserialize</param>
        /// <param name="encoding">encoding of the file</param>
        /// <returns>an object of type T</returns>
        public static T LoadFromFile<T>(string filePath, System.Text.Encoding encoding)
        {
            System.IO.FileStream file = null;
            System.IO.StreamReader sr = null;
            try
            {

                if (filePath.Contains(Path.DirectorySeparatorChar) && !Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                }
                if (!File.Exists(filePath))
                {
                    byte[] fileBytes;
                    string xmlFromRecource = "";
                    string typeName = typeof(T).Name;
                    string[] resourceNames = typeof(T).Assembly.GetManifestResourceNames();
                    foreach (string resourceName in resourceNames)
                    {
                        Stream stream = typeof(T).Assembly.GetManifestResourceStream(resourceName);
                        using (BinaryReader br = new BinaryReader(stream))
                        {
                            fileBytes = br.ReadBytes((int)stream.Length);
                        }
                        string ressourceAsString = Encoding.UTF8.GetString(fileBytes);

                        int startTagPosition = ressourceAsString.IndexOf("<" + typeName);
                        int endTagPosition = ressourceAsString.IndexOf("</" + typeName + ">");

                        if (startTagPosition > 0 && endTagPosition > startTagPosition)
                        {
                            int extractionLenght = endTagPosition - startTagPosition + typeName.Length + 4;
                            xmlFromRecource = ressourceAsString.Substring(startTagPosition, extractionLenght);
                            break;
                        }
                    }
                    if (xmlFromRecource == "")
                    {

                    }

                    if (xmlFromRecource != "")
                    {
                        File.WriteAllText(filePath, xmlFromRecource);
                        Console.WriteLine("Wite xmlFromResrouce to: " + filePath);
                    }
                }
                file = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read);
                sr = new System.IO.StreamReader(file, encoding);
                string xmlString = sr.ReadToEnd();
                sr.Close();
                file.Close();
                return Deserialize<T>(xmlString);
            }
            finally
            {
                if ((file != null))
                {
                    file.Dispose();
                }
                if ((sr != null))
                {
                    sr.Dispose();
                }
            }
        }
    }

}
