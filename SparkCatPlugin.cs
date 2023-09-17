using BepInEx;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using UnityEngine;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System;
using MoreSlugcats;
using SlugBase.Assets;
using Menu;
using System.Reflection;
using System.Security.AccessControl;
using RWCustom;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace SparkCat
{

    [BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("phace.electricrubbish", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("improved-input-config", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.impulse";
        public const string PLUGIN_NAME = "Impulse";
        public const string PLUGIN_VERSION = "0.5.3";
        public const string SLUGCAT_NAME = "sparkcat";

        public static readonly PlayerFeature<float> SparkJump = PlayerFloat("spark_jump");

        public OptionInterface config;

        public static Dictionary<Player, SparkCatState> states;

        public static bool _custom_input_enabled = false;
        public static bool custom_input_enabled(int player)
        {
            return _custom_input_enabled && (zipKeybind.GetType().GetMethod("CurrentBinding").Invoke( zipKeybind, new object[] { player }) as KeyCode? ?? KeyCode.None) != KeyCode.None;
        }
        public static object zipKeybind;
        public static bool custom_zip_pressed(int player)
        {
            return zipKeybind.GetType().GetMethod("CheckRawPressed").Invoke(zipKeybind, new object[] { player}) as bool? ?? false;
        }

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
            On.Menu.MainMenu.ctor += MainMenuCheckDependency;

            Enums.Initialize();
        }

        private void MainMenuCheckDependency(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            Version v = Version.Parse(ElectricRubbish.ElectricRubbishMain.plugin_live_version);
            if(v < new Version(1, 3, 2))
            {
                self.popupAlert = new DialogBoxNotify(self, self.pages[0],
                    "Impulse: Dependency version too low. ElectricRubbish is out of date. Please update. Req: 1.3.2 Was: " + v, "ALERT",
                    new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 284f), new Vector2(480f, 180f), false);
                self.pages[0].subObjects.Add(self.popupAlert);
                self.manager.rainWorld.HandleLog("Impulse: ElectricRubbish is out of date. Should be at least 1.3.2, was " + v + ".", "SparkCatPlugin.InitHook", LogType.Error);
            }
        }

        private void InitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            config = new SparkCatOptions();
            MachineConnector.SetRegisteredOI(PLUGIN_GUID, config);

            var inputconfig = ModManager.ActiveMods.Find((ModManager.Mod m) =>
            {
                return m.id == "improved-input-config";
            });
            if (inputconfig != null)
            {
                _custom_input_enabled = true;
                zipKeybind = Type.GetType("ImprovedInput.PlayerKeybind,ImprovedInput").GetMethod("Get").Invoke(null, new object[] { "impulse:zip" });
                if(zipKeybind == null)
                    zipKeybind = Type.GetType("ImprovedInput.PlayerKeybind,ImprovedInput").
                        GetMethod("Register", new[] { typeof(string), typeof(string), typeof(string), typeof(KeyCode), typeof(KeyCode) }).
                        Invoke(null, new object[] { "impulse:zip", "The Impulse", "Zip Override", KeyCode.None, KeyCode.None });
                Debug.Log("Impulse: custom input settings connected.");
            }
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
            if (self is Player p && SparkJump.TryGet(p, out float jumpStrength) && jumpStrength > 0)
            {
                if (type == Creature.DamageType.Electric && (source == null || !(source.owner is ElectricRubbish.ElectricRubbish)))
                {
                    states[p].rechargeZipStorage(4);
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private void PlayerGrabHook(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            if (SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                states[self].chargeablesState.GrabHook(orig, self, eu);
                Maul.GrabBehavior(self, eu, states[self]);
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
