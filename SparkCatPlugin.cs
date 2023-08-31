
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
            On.Player.Destroy += DestroyHook;
            On.PlayerGraphics.ctor += GraphicsInitHook;
            On.PlayerGraphics.DrawSprites += PlayerGraphicsHook;
            states = new Dictionary<int, SparkCatState>();
        }


        private void GraphicsInitHook(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            Debug.Log("init graphic");
            if (ow is Player p)
            {
                if (SparkJump.TryGet(p, out float jumpStrength) && jumpStrength > 0)
                {
                    states[p.playerState.playerNumber] = new SparkCatState(p, self);
                }
            }
            orig(self, ow);
        }

        private void DestroyHook(On.Player.orig_Destroy orig, Player self)
        {
            if (states.ContainsKey(self.playerState.playerNumber))
                states.Remove(self.playerState.playerNumber);
            orig(self);
        }

        public void UpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            if(SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.playerState.playerNumber].ClassMechanicsSparkCat(jumpStrength);
                states[self.playerState.playerNumber].DoZip();
            }
            orig(self, eu);
        }

        private void PlayerGraphicsHook(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            foreach(var g in states.Keys)
            {
                if (states[g].graphic_teleporting)
                {
                    orig(self, sLeaser, rCam, 1, camPos);
                    return;
                }
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }
    }
}
