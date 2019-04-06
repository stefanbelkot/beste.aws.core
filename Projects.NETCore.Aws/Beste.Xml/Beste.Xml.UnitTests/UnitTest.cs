using Beste.Xml.UnitTests.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Linq;

namespace Beste.Xml.UnitTests
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void WriteLoadAndCompareXml()
        {
            XmlTestClass xmlObject = new XmlTestClass();
            xmlObject.AnotherTestString = "AnotherTestString";
            xmlObject.TestString = "TestString";
            xmlObject.TestInteger = 1;
            xmlObject.TestDouble = 1.11;
            xmlObject.TestSubClass = new SubClass();
            xmlObject.TestSubClass.TestString = "SubTestString";
            xmlObject.TestSubClass.TestList = new System.Collections.Generic.List<string>();
            xmlObject.TestSubClass.TestList.Add("TestListEntry");
            xmlObject.TestSubClass.TestList.Add("AnotherTestListEntry");
            
            xmlObject.SaveToFile("test.xml");

            XmlTestClass loadedXmlObject = XmlTestClass.LoadFromFile<XmlTestClass>("test.xml");

            if (!loadedXmlObject.Equals(xmlObject))
                Assert.Fail();

        }

        [TestMethod]
        public void ReadDbSettingsXml()
        {
            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            DbSettings dbSettings = DbSettings.LoadFromFile<DbSettings>(pathToConfig + "DBConnectionSettings.xml");
        }
        
        [TestMethod]
        public void ReadDbSettingsXmlByOut()
        {

            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            bool success = DbSettings.LoadFromFile(pathToConfig + "DBConnectionSettings.xml", out DbSettings dbSettings);
            if (!success)
                Assert.Fail();
        }

        [TestMethod]
        public void SaveDbSettings()
        {
            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            DbSettings dbSettings = new DbSettings
            {
                Ip = "test",
                DbUser = "test",
                DbSchema = "test",
                DbPassword = "test"
            };
            System.Exception ex;
            bool success = dbSettings.SaveToFile(pathToConfig + "DBConnectionSettings_test.xml", out ex);

        }

        //[TestMethod]
        //public void NotExistingXmlCreateByConsoleTester()
        //{
        //    string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
        //    string filePath = pathToConfig + "DBConnectionSettings.xml";
        //    if (File.Exists(filePath))
        //        File.Delete(filePath);

        //    ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        //    startInfo.Arguments = "ConsoleTester" + Path.DirectorySeparatorChar + "ConsoleTester.dll";
        //    Process myProcess;
        //    myProcess = Process.Start(startInfo);
        //    myProcess.WaitForExit();

        //    DbSettings dbSettings;
        //    Exception ex;
            
        //    bool success = DbSettings.LoadFromFile<DbSettings>(filePath, out dbSettings, out ex);
        //    if (!success)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        Assert.Fail();
        //    }
        //}


        [TestMethod]
        public void NotExistingXmlCreateFromResource()
        {
            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            string filePath = pathToConfig + "DBConnectionSettings.xml";
            if (File.Exists(filePath))
                File.Delete(filePath);

            // To be able to add the recource in the assembly it must be defined in the .csproj:
            // <ItemGroup>
            //    (...)
            //    <EmbeddedResource Include="Resources\DBConnectionSettings.xml" />
            // </ItemGroup>
            var assembly = typeof(UnitTest).GetTypeInfo().Assembly;
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
                Console.WriteLine("Found Resource with name: " + resourceName);
            string dbConXmlRessourceName = resourceNames.SingleOrDefault(x => x.Contains("DBConnection"));
            if(dbConXmlRessourceName == "")
            {
                Console.WriteLine("Needed Resource not found!");
                Assert.Fail();
            }
            Stream resourceStream = assembly.GetManifestResourceStream(dbConXmlRessourceName);
            resourceStream.WriteAllToFile(filePath);
            resourceStream.Dispose();
            
            bool success = DbSettings.LoadFromFile<DbSettings>(filePath, out DbSettings dbSettings, out Exception ex);
            if (!success)
            {
                Console.WriteLine(ex.ToString());
                Assert.Fail();
            }
        }

        [TestMethod]
        public void NotExistingXmlNotExistingResource()
        {

            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            string filePath = pathToConfig + "DBConnectionSettings_NotExistingXmlNotExistingResource.xml";
            UnitTest unitTest;
            Exception ex;
            bool success = DbSettings.LoadFromFile<UnitTest>(filePath + "_UnitTest.xml", out unitTest, out ex);
            if (success)
                Assert.Fail();
        }

    }
}
