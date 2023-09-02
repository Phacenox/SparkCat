
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
            On.HUD.FoodMeter.QuarterPipShower.Update += QuarterPipReductionHook;

            On.Player.ctor += PlayerInitHook;
            On.Player.Update += UpdateHook;
            On.Player.Destroy += DestroyHook;
            On.Player.InitiateGraphicsModule += InitGraphicsTypeHook;

            Sounds.Initialize();
        }

        private void PlayerInitHook(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.playerState.playerNumber] = new SparkCatState(self);
            }
        }

        private void InitGraphicsTypeHook(On.Player.orig_InitiateGraphicsModule orig, Player self)
        {
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                if (self.graphicsModule == null)
                {
                    self.graphicsModule = new SparkCatGraphics(self, states[self.playerState.playerNumber]);
                    states[self.playerState.playerNumber].graphics = self.graphicsModule as SparkCatGraphics;
                }
            }
            else
            {
                orig(self);
            }
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

        private void PlayerAddHook(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (SparkJump.TryGet(self.player, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.player.playerState.playerNumber].graphics.AddToContainer(sLeaser, rCam, newContatiner);
            }
            orig(self, sLeaser, rCam, newContatiner);
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
