using System;
using System.Collections.Generic;
using System.Text;

namespace Beste.GameServer.SDaysTDie.Modules
{
    using Beste.Databases.User;
    using Beste.GameServer.SDaysTDie.Modules.Types;
    using FluentNHibernate.Mapping;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ServerSettings : Beste.Xml.Xml
    {

        /// <summary>
        /// Representation of the serverconfig.xml
        /// </summary>
        [XmlElement("property")]
        public List<Property> Property { get; set; }

        /// <summary>
        /// FilePath of the ServerConfigFile
        /// </summary>
        [XmlIgnore]
        public string ServerConfigFilepath { get; set; } = "serverconfig.xml";

        #region "Forbid save as serverconfig.xml"
        public override void SaveToFile(string fileName)
        {
            if (fileName.EndsWith("serverconfig.xml"))
                throw new ArgumentException("File is not allowed to be saved as 'serverconfig.xml'");
            base.SaveToFile(fileName);
        }
        public override void SaveToFile(string fileName, Encoding encoding)
        {
            if (fileName.EndsWith("serverconfig.xml"))
                throw new ArgumentException("File is not allowed to be saved as 'serverconfig.xml'");
            base.SaveToFile(fileName, encoding);
        }
        public override bool SaveToFile(string fileName, Encoding encoding, out Exception exception)
        {
            if (fileName.EndsWith("serverconfig.xml"))
                throw new ArgumentException("File is not allowed to be saved as 'serverconfig.xml'");
            return base.SaveToFile(fileName, encoding, out exception);
        }
        public override bool SaveToFile(string fileName, out Exception exception)
        {
            if (fileName.EndsWith("serverconfig.xml"))
                throw new ArgumentException("File is not allowed to be saved as 'serverconfig.xml'");
            return base.SaveToFile(fileName, out exception);
        }
        #endregion
        #region "Representation of the configurable serverconfig.xml as properties"
        //Server representation
        [XmlIgnore]
        public string ServerName
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return GetPropertyValueFromStacktrace(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value); }
        }
        [XmlIgnore]
        public string ServerDescription
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return GetPropertyValueFromStacktrace(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value); }
        }
        [XmlIgnore]
        public string ServerPassword
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return GetPropertyValueFromStacktrace(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value); }
        }

        //Server Networking
        [XmlIgnore]
        public int ServerPort
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return Convert.ToInt32(GetPropertyValueFromStacktrace()); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value.ToString()); }
        }
        //Admin interfaces
        [XmlIgnore]
        public int TelnetPort
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return Convert.ToInt32( GetPropertyValueFromStacktrace()); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value.ToString()); }
        }
        [XmlIgnore]
        public string TelnetPassword
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return GetPropertyValueFromStacktrace(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value); }
        }
        [XmlIgnore]
        public bool TerminalWindowEnabled
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return Convert.ToBoolean(GetPropertyValueFromStacktrace()); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value.ToString().ToLower()); }
        }
        [XmlIgnore]
        //World
        public GameWorld GameWorld
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return (GameWorld)Enum.Parse(typeof(GameWorld), GetPropertyValueFromStacktrace()) ; }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value.ToString()); }
        }
        public string WorldGenSeed
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return GetPropertyValueFromStacktrace(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value); }
        }
        public string GameName
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return GetPropertyValueFromStacktrace(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { SetPropertyValueFromStacktrace(value); }
        }
        #endregion

        #region "DataBase only fields"
        [XmlIgnore]
        public int Id { get; set; }
        [XmlIgnore]
        public User User { get; set; }
        #endregion
        #region "Extracting of the Property from stacktrace"

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetPropertyValueFromStacktrace()
        {
            Property property = GetPropertyFromStacktrace();
            return property.Value;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetPropertyValueFromStacktrace(string value)
        {
            Property property = GetPropertyFromStacktrace();
            property.Value = value;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Property GetPropertyFromStacktrace()
        {
            string propertyName = CurrentPropertyName();
            Property property = Property.SingleOrDefault(x => x.Name == propertyName);
            if (property == null)
            {
                throw new ArgumentException("The property='" + propertyName + "' does not exist in the XML! It cant be accessed!");
            }
            return property;
        }

        private static string CurrentPropertyName()
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            var thisFrame = frames[3];
            var method = thisFrame.GetMethod();
            var methodName = method.Name; // Should be get_* or set_*
            var propertyName = method.Name.Substring(4);
            return propertyName;
        }
        #endregion

        public virtual void FromServerSetting(ServerSetting source)
        {
            ServerName = source.ServerName;
            ServerDescription = source.ServerDescription;
            ServerPassword = source.ServerPassword ;
            ServerPort = source.ServerPort;
            TelnetPort = source.TelnetPort;
            TelnetPassword = source.TelnetPassword;
            TerminalWindowEnabled = source.TerminalWindowEnabled;
            GameWorld = source.GameWorld;
            WorldGenSeed = source.WorldGenSeed;
            GameName = source.GameName;
            User =  source.User;
        }
    }

    /// <remarks/>
    public partial class Property
    {

        [System.Xml.Serialization.XmlAttribute(AttributeName = "name")]
        public string Name { set; get; }

        [System.Xml.Serialization.XmlAttribute(AttributeName = "value")]
        public string Value { set; get; }
        
    }

    public class ServerSettingMap : ClassMap<ServerSetting>
    {
        public ServerSettingMap()
        {
            Table("server_setting");
            LazyLoad();
            Id(x => x.Id).GeneratedBy.Identity().Column("id");
            References(x => x.User).Column("fk_user_id");
            Map(x => x.ServerConfigFilepath).Column("server_config_filepath").Not.Nullable();
            Map(x => x.ServerName).Column("server_name").Not.Nullable();
            Map(x => x.ServerDescription).Column("server_description").Not.Nullable();
            Map(x => x.ServerPassword).Column("server_password").Not.Nullable();
            Map(x => x.ServerPort).Column("server_port").Not.Nullable();
            Map(x => x.TelnetPort).Column("telnet_port").Not.Nullable();
            Map(x => x.TelnetPassword).Column("telnet_password").Not.Nullable();
            Map(x => x.TerminalWindowEnabled).Column("terminal_window_enabled").Not.Nullable();
            Map(x => x.GameWorld).Column("game_world").Not.Nullable();
            Map(x => x.WorldGenSeed).Column("world_gen_seed").Not.Nullable();
            Map(x => x.GameName).Column("game_name").Not.Nullable();
        }
    }

    public class ServerSetting
    {
        public virtual string ServerConfigFilepath { get; set; }
        public virtual bool IsRunning { get; set; }

        #region "Representation of the configurable serverconfig.xml as properties"

        [XmlIgnore]
        public virtual string ServerName { get; set; }
        public virtual string ServerDescription { get; set; }
        public virtual string ServerPassword { get; set; }
        public virtual int ServerPort { get; set; }
        public virtual int TelnetPort { get; set; }
        public virtual string TelnetPassword { get; set; }
        public virtual bool TerminalWindowEnabled { get; set; }
        public virtual GameWorld GameWorld { get; set; }
        public virtual string WorldGenSeed { get; set; }
        public virtual string GameName { get; set; }
        #endregion

        #region "DataBase only fields"
        public virtual int Id { get; set; }
        public virtual User User { get; set; }
        #endregion
        public virtual void CopyAllButId(ServerSetting target)
        {
            target.ServerName = ServerName;
            target.ServerDescription = ServerDescription;
            target.ServerPassword = ServerPassword;
            target.ServerPort = ServerPort;
            target.TelnetPort = TelnetPort;
            target.TelnetPassword = TelnetPassword;
            target.TerminalWindowEnabled = TerminalWindowEnabled;
            target.GameWorld = GameWorld;
            target.WorldGenSeed = WorldGenSeed;
            target.GameName = GameName;
            target.User = User;
        }
    }
}
