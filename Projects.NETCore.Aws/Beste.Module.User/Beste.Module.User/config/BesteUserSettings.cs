using System;
using System.Collections.Generic;
using System.Text;

namespace Beste.Aws.Module.Settings
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class BesteUserSettings : Beste.Xml.Xml
    {
        public MandatoryUserParams MandatoryUserParams { get; set; } = new MandatoryUserParams();
        public PasswordRules PasswordRules { get; set; } = new PasswordRules();

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class MandatoryUserParams
    {
        public bool Firstname { get; set; } = true;
        public bool Lastname { get; set; } = true;
        public bool EMail { get; set; } = true;
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class PasswordRules
    {
        public int MinLength { get; set; } = 0;
        public bool HasDigit { get; set; } = true;
        public bool HasLowerCase { get; set; } = true;
        public bool HasUpperCase { get; set; } = true;
        public bool HasSpecialChars { get; set; } = true;
    }
}
