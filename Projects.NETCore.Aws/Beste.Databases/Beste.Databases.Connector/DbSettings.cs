namespace Beste.Databases.Connector
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class DbSettings : Beste.Xml.Xml
    {

        /// <remarks/>
        public string Ip { get; set; }

        /// <remarks/>
        public string DbSchema { get; set; }

        /// <remarks/>
        public string DbUser { get; set; }

        /// <remarks/>
        public string DbPassword { get; set; }
    }
}
