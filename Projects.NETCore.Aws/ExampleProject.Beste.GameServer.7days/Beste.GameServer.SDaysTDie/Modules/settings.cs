﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// Dieser Quellcode wurde automatisch generiert von xsd, Version=4.6.1055.0.
// 
namespace Beste.GameServer.SDaysTDie {
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
    public partial class Settings : Beste.Xml.Xml {
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Endpoint", IsNullable = false)]
        public SettingsEndpoint[] Endpoints { get; set; }
        
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
    public partial class SettingsEndpoint {

        /// <remarks/>
        public string Host { set; get; }
        
        /// <remarks/>
        public int? Port { set; get; }


        /// <remarks/>
        public string Scheme { set; get; }


        /// <remarks/>
        public string StoreName { set; get; }


        /// <remarks/>
        public string StoreLocation { set; get; }


        /// <remarks/>
        public string FilePath { set; get; }


        /// <remarks/>
        public string Password { set; get; }

    }
}