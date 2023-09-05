using BepInEx;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using UnityEngine;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System;
using MoreSlugcats;

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
        public const string PLUGIN_GUID = "phace.impulse";
        public const string PLUGIN_NAME = "Impulse";
        public const string PLUGIN_VERSION = "0.4.0";
        public const string SLUGCAT_NAME = "sparkcat";

        public static readonly PlayerFeature<float> SparkJump = PlayerFloat("spark_jump");

        public OptionInterface config;

        public static Dictionary<Player, SparkCatState> states;

        public void OnEnable()
        {
            states = new Dictionary<Player, SparkCatState>();

            On.RainWorld.OnModsInit += InitHook;

            On.Player.ctor += PlayerInitHook;
            On.Player.Update += UpdateHook;
            On.Player.Destroy += DestroyHook;
            On.Player.InitiateGraphicsModule += InitGraphicsTypeHook;
            On.Player.ReleaseObject += ReleaseObjectHook;

            On.Player.GrabUpdate += PlayerGrabHook;
            On.Creature.Violence += CreatureViolenceHook;

            //Crafting
            On.Player.CraftingResults += Crafting.CraftingResultHook;
            On.Player.GraspsCanBeCrafted += Crafting.CanBeCraftedHook;
            On.Player.SpitUpCraftedObject += Crafting.SpitUpCraftedHook;

            On.StoryGameSession.ctor += setCampaignHook;

            On.SSOracleBehavior.PebblesConversation.AddEvents += Oracle.PebblesConversationAddEventsHook;
            On.SSOracleBehavior.SeePlayer += Oracle.OracleSeePlayerHook;
            On.SSOracleBehavior.SSSleepoverBehavior.ctor += Oracle.SSSleepoverBehaviorctorHook;

            On.SSOracleBehavior.Update += Oracle.SSUpdateHook;

            Enums.Initialize();
        }


        private void InitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            config = new SparkCatOptions();
            MachineConnector.SetRegisteredOI(PLUGIN_GUID, config);
        }

        private void ReleaseObjectHook(On.Player.orig_ReleaseObject orig, Player self, int grasp, bool eu)
        {
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self].ReleaseObjectHook(orig, self, grasp, eu);
            }
            else
            {
                orig(self, grasp, eu);
            }
        }

        private void setCampaignHook(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            if (SparkCatOptions.ConstrainElectricRubbish == SparkCatOptions.CONSTRAIN.Disable || (SparkCatOptions.ConstrainElectricRubbish == SparkCatOptions.CONSTRAIN.ImpulseCampaign && saveStateNumber.value != SLUGCAT_NAME))
                ElectricRubbish.ElectricRubbishOptions.replaceRateScalar = 0;
            else
                ElectricRubbish.ElectricRubbishOptions.replaceRateScalar = 1;
            orig(self, saveStateNumber, game);
        }

        private void CreatureViolenceHook(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if(self is Player p && SparkJump.TryGet(p, out float jumpStrength) && jumpStrength > 0)
            {
                if(type == Creature.DamageType.Electric && !(source.owner is ElectricRubbish.ElectricRubbish))
                {
                    states[p].rechargeZipStorage(4);
                    damage /= 2;
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private void PlayerGrabHook(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self].chargeablesState.GrabHook(orig, self, eu);
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
                states[self] = new SparkCatState(self);
            }
        }

        private void InitGraphicsTypeHook(On.Player.orig_InitiateGraphicsModule orig, Player self)
        {
            if(self.SlugCatClass.value == SLUGCAT_NAME)
            {
                if (self.graphicsModule == null)
                    self.graphicsModule = new SparkCatGraphics(self, states[self]);
            }
            else
            {
                orig(self);
            }
        }

        private void DestroyHook(On.Player.orig_Destroy orig, Player self)
        {
            
            if (states.ContainsKey(self))
                states.Remove(self);
            
            orig(self);
        }

        public void UpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self].chargeablesState.Update();
                states[self].ClassMechanicsSparkCat(jumpStrength);
                states[self].Update();
                states[self].DoZip();
            }
            orig(self, eu);
        }
    }
}
