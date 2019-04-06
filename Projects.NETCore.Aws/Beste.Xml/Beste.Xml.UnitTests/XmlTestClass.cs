using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beste.Xml.UnitTests
{

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class XmlTestClass : Xml
    {

        public string TestString { get; set; }
        public string AnotherTestString { get; set; }
        public int TestInteger { get; set; }
        public double TestDouble { get; set; }
        public SubClass TestSubClass { get; set; }

        public override bool Equals(object obj)
        {
            var @class = obj as XmlTestClass;
            return @class != null &&
                   TestString == @class.TestString &&
                   AnotherTestString == @class.AnotherTestString &&
                   TestInteger == @class.TestInteger &&
                   TestDouble == @class.TestDouble &&
                   EqualityComparer<SubClass>.Default.Equals(TestSubClass, @class.TestSubClass);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TestString, AnotherTestString, TestInteger, TestDouble, TestSubClass);
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class SubClass
    {
        public string TestString { get; set; }
        public List<string> TestList { get; set; }

        public override bool Equals(object obj)
        {
            var @class = obj as SubClass;
            return @class != null &&
                   TestString == @class.TestString &&
                   TestList.SequenceEqual(@class.TestList);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TestString, TestList);
        }
    }
}

