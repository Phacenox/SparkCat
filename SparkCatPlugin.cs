
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
using IL.MoreSlugcats;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace SparkCat
{
    [BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("phace.electricrubbish", BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.sparkcat";
        public const string PLUGIN_NAME = "Impulse";
        public const string PLUGIN_VERSION = "0.2.2";

        public static readonly PlayerFeature<float> SparkJump = PlayerFloat("spark_jump");

        public static Dictionary<int, SparkCatState> states;

        public void OnEnable()
        {
            states = new Dictionary<int, SparkCatState>();

            On.Player.ctor += PlayerInitHook;
            On.Player.Update += UpdateHook;
            On.Player.Destroy += DestroyHook;
            On.Player.InitiateGraphicsModule += InitGraphicsTypeHook;

            On.Player.GrabUpdate += PlayerGrabHook;
            On.Creature.Violence += CreatureViolenceHook;

            //Crafting
            On.Player.CraftingResults += Crafting.CraftingResultHook;
            On.Player.GraspsCanBeCrafted += Crafting.CanBeCraftedHook;
            On.Player.SpitUpCraftedObject += Crafting.SpitUpCraftedHook;

            On.StoryGameSession.ctor += setCampaignHook;

            Sounds.Initialize();
        }

        private void setCampaignHook(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            /* TODO: this is default, add to mod options.
            if (saveStateNumber.value != "sparkcat")
                ElectricRubbish.ElectricRubbishOptions.replaceRateScalar = 0;*/
            orig(self, saveStateNumber, game);
        }

        private void CreatureViolenceHook(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if(self is Player p && SparkJump.TryGet(p, out float jumpStrength) && jumpStrength > 0)
            {
                if(type == Creature.DamageType.Electric && !(source.owner is ElectricRubbish.ElectricRubbish))
                {
                    states[p.playerState.playerNumber].rechargeZipStorage(4);
                    damage /= 2;
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private void PlayerGrabHook(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self.playerState.playerNumber].chargeablesState.GrabHook(orig, self, eu);
            }
            else
            {
                orig(self, eu);
            }
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
            if(self.SlugCatClass.value == "sparkcat")
            {
                if (self.graphicsModule == null)
                    self.graphicsModule = new SparkCatGraphics(self, states[self.playerState.playerNumber]);
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
                states[self.playerState.playerNumber].chargeablesState.Update();
                states[self.playerState.playerNumber].ClassMechanicsSparkCat(jumpStrength);
                states[self.playerState.playerNumber].DoZip();
            }
            orig(self, eu);
        }
    }
}
