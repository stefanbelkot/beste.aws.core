using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Beste.Rights
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Settings : Beste.Xml.Xml
    {
        public TokenInterval TokenInitialTimeout { get; set; }
        public TokenInterval TokenRefreshOnUsage { get; set; }
        
    }
    public partial class TokenInterval
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public string TokenRefreshOnUsage { get; set; }

    }


    internal static class SettingsManager
    {
        internal static Settings LoadSettings(string settingsPath)
        {
            string directoryPath = new FileInfo(settingsPath).Directory.FullName;
            if (!Directory.Exists(directoryPath) || !File.Exists(settingsPath))
            {
                CreateDefaultSettings(settingsPath);
            }
            return Settings.LoadFromFile<Settings>(settingsPath);
        }
        internal static void CreateDefaultSettings(string settingsPath)
        {
            string directoryPath = new FileInfo(settingsPath).Directory.FullName;
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(settingsPath))
            {

                var assembly = typeof(RightControl).GetTypeInfo().Assembly;
                string[] names = assembly.GetManifestResourceNames();
                Stream resourceStream = assembly.GetManifestResourceStream("Beste.Aws.Rights.Resources.Beste.Rights.Settings.xml");
                resourceStream.WriteAllToFile(settingsPath);
                resourceStream.Dispose();
            }
        }
    }
}
