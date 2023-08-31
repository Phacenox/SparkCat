using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using Noise;
using BepInEx;

namespace SparkCat
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.sparkcat";
        public const string PLUGIN_NAME = "Impulse";
        public const string PLUGIN_VERSION = "0.0.1";
    }
}
