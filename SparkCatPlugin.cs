
using BepInEx;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace SparkCat
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.sparkcat";
        public const string PLUGIN_NAME = "Impulse";
        public const string PLUGIN_VERSION = "0.0.1";

        static readonly PlayerFeature<float> SparkJump = PlayerFloat("spark_jump");

        Dictionary<int, SparkCatState> states;

        public void OnEnable()
        {
            On.Player.Update += UpdateHook;
            On.Player.ctor += CreateHook;
            On.Player.Destroy += DestroyHook;
            states = new Dictionary<int, SparkCatState>();
        }

        private void DestroyHook(On.Player.orig_Destroy orig, Player self)
        {
            if (states.ContainsKey(self.playerState.playerNumber))
                states.Remove(self.playerState.playerNumber);
            orig(self);
        }

        private void CreateHook(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if(SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.playerState.playerNumber] = new SparkCatState(self);
            }
        }

        public void UpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            if(SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.playerState.playerNumber].ClassMechanicsSparkCat(jumpStrength);
                states[self.playerState.playerNumber].DoSparkJump();
            }
            orig(self, eu);
        }
    }
}
