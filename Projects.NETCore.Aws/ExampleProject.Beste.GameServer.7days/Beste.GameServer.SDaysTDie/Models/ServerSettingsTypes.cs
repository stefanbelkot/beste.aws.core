using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Beste.GameServer.SDaysTDie.Modules.Types
{

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameWorld
    {
        RWG,
        Navezgane
    }

}
