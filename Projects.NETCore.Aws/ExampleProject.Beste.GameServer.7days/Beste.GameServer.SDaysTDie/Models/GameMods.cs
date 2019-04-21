using System;
using System.Collections.Generic;
using System.Text;

namespace Beste.GameServer.SDaysTDie.Models
{
    public partial class GameMods : Beste.Xml.Xml
    {

        public List<GameMod> ConfiguredGameMods { get; set; }

    }
    public class GameMod
    {
        [System.Xml.Serialization.XmlAttribute(AttributeName = "ModName")]
        public string ModName { set; get; }

        [System.Xml.Serialization.XmlAttribute(AttributeName = "PathToGameFiles")]
        public string PathToGameFiles { set; get; }
    }
}
