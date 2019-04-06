using Beste.Xml.UnitTests.Test;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace ConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            string filePath = pathToConfig + "DBConnectionSettings.xml";
            DbSettings dbSettings;
            Exception ex;
            if(File.Exists(filePath))
                File.Delete(filePath);
            bool success = DbSettings.LoadFromFile<DbSettings>(filePath, out dbSettings, out ex);
        }
    }
}
