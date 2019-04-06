using System;

namespace Beste.Xml
{
    interface IXml
    {

        /// <summary>
        /// Serializes object into an XML document
        /// </summary>
        /// <param name="encoding">encoding of the xml</param>
        /// <returns>string XML value</returns>
        string Serialize(System.Text.Encoding encoding);

        /// <summary>
        /// Serializes object into an XML document with default encoding UTF8
        /// </summary>
        /// <returns>string XML value</returns>
        string Serialize();

        /// <summary>
        /// Serializes the object into file
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        /// <param name="encoding">encoding of the file</param>
        /// <param name="exception">output Exception value if failed</param>
        /// <returns>true if can serialize and save into file; otherwise, false</returns>
        bool SaveToFile(string fileName, System.Text.Encoding encoding, out System.Exception exception);

        /// <summary>
        /// Serializes the object into file with default encoding UTF8
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        /// <param name="exception">output Exception value if failed</param>
        /// <returns>true if can serialize and save into file; otherwise, false</returns>
        bool SaveToFile(string fileName, out System.Exception exception);

        /// <summary>
        /// Serializes the object into file with default encoding UTF8
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        void SaveToFile(string fileName);

        /// <summary>
        /// Serializes the object into file
        /// </summary>
        /// <param name="fileName">full path of outupt xml file</param>
        /// <param name="encoding">encoding of the file</param>
        void SaveToFile(string fileName, System.Text.Encoding encoding);

    }
}
