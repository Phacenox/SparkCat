
using BepInEx;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using UnityEngine;
using System;
using System.Collections.Generic;
using IL.JollyCoop.JollyMenu;
using System.Security;
using System.Security.Permissions;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SparkCat
{
    [BepInDependency("phace.electricrubbish", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]

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
            states = new Dictionary<int, SparkCatState>();
            On.PlayerGraphics.ctor += GraphicsInitHook;
            On.Player.Update += UpdateHook;
            On.PlayerGraphics.DrawSprites += PlayerGraphicsHook;
            On.Player.Destroy += DestroyHook;
            On.HUD.FoodMeter.QuarterPipShower.Update += QuarterPipReductionHook;
        }

        public void Awake()
        {
            Sounds.Initialize();
        }

        private void GraphicsInitHook(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
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
            if(SparkJump.TryGet(self.player, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.player.playerState.playerNumber].graphics.DrawSpritesOverride(orig, sLeaser, rCam, timeStacker, camPos);
            }
            else
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
            }
        }

        private void QuarterPipReductionHook(On.HUD.FoodMeter.QuarterPipShower.orig_Update orig, HUD.FoodMeter.QuarterPipShower self)
        {
            if (self.owner.hud.owner is Player p)
            {
                if (self.displayQuarterFood > p.playerState.quarterFoodPoints)
                {
                    self.Reset();
                }
            }
            orig(self);
        }
    }
}
