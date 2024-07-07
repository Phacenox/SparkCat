using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System.Collections.Generic;
using UnityEngine;

namespace SparkCat
{
    public class SparkCatOptions: OptionInterface
    {
        public static Configurable<bool> Enable_Cheats;
        public static Configurable<bool> No_Food_Cost;
        public static bool NoFoodCost => Enable_Cheats.Value && No_Food_Cost.Value;
        public static Configurable<bool> Always_Overcharge;
        public static bool AlwaysOvercharge => Enable_Cheats.Value && Always_Overcharge.Value;
        public static Configurable<bool> Zip_Through_Walls;
        public static bool ZipThroughWalls => Enable_Cheats.Value && Zip_Through_Walls.Value;
        public static Configurable<int> Zip_Length;
        public static int ZipLength => Enable_Cheats.Value ? Zip_Length.Value : 140;

        public static Configurable<string> Constrain_Electric_Rubbish;
        public enum  CONSTRAIN
        {
            Everywhere,
            ImpulseCampaign,
            Disable
        }
        static List<string> constrainOptions;
        public static CONSTRAIN ConstrainElectricRubbish
        {
            get
            {
                var r = constrainOptions.IndexOf(Constrain_Electric_Rubbish.Value);
                if (r >= 0)
                    return (CONSTRAIN)r;
                return CONSTRAIN.ImpulseCampaign;
            }
        }

        public SparkCatOptions() 
        {
            constrainOptions = new List<string>
            {
                "In all Campaigns",
                "Impulses Campaign",
                "Nowhere"
            };
            Constrain_Electric_Rubbish = config.Bind<string>("Constrain_Electric_Rubbish", "Impulses Campaign", new ConfigAcceptableList<string>(constrainOptions.ToArray()));
            Enable_Cheats = config.Bind<bool>("Enable_Cheats", false);
            No_Food_Cost = config.Bind<bool>("No_Food_Cost", false);
            Always_Overcharge = config.Bind<bool>("Always_Overcharge", false);
            Zip_Length = config.Bind<int>("Zip_Length", 140, new ConfigAcceptableRange<int>(10, 400));
            Zip_Through_Walls = config.Bind<bool>("Zip_Through_Walls", false);
        }

        OpRect cheatcoverrect;
        OpCheckBox cheatenable;
        public override void Update()
        {
            base.Update();
            cheatcoverrect.fillAlpha = cheatenable.GetValueBool() ? 0 : 0.7f;
        }

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new[]
            {
                new OpTab(this)
            };
            OpLabel Label1 = new OpLabel(0f, 550f, "Spawn Electric Rubbish");
            OpListBox listbox = new OpListBox(Constrain_Electric_Rubbish, new UnityEngine.Vector2(0f, 450f), 150, 3);
            listbox.description = "Choose wheter Electric Rubbish spawns are limited to a specific campaign.";
            listbox._itemList[0].desc = "Electric Rubbish is active in every room.";
            listbox._itemList[1].desc = "Electric Rubbish is only active in Impulse's campaign.";
            listbox._itemList[2].desc = "Electric Rubbish is never active.";

            cheatenable = new OpCheckBox(Enable_Cheats, new Vector2(300, 550));
            OpLabel Label2 = new OpLabel(330f, 550f, "Enable Cheats");

            cheatcoverrect = new OpRect(new Vector2(280, 410), new Vector2(180, 130), 0);
            

            OpCheckBox check1 = new OpCheckBox(No_Food_Cost, new Vector2(300, 510));
            check1.description = "Removes the food cost for recharging and for all crafts.";
            OpLabel checklabel1 = new OpLabel(330f, 510f, "Free recharge.");

            OpCheckBox check2 = new OpCheckBox(Always_Overcharge, new Vector2(300, 480));
            check2.description = "Zips recharge extremely quickly, even in the air.";
            OpLabel checklabel2 = new OpLabel(330f, 480f, "Always overcharged.");

            OpDragger dragger = new OpDragger(Zip_Length, new Vector2(300, 450));
            OpLabel checklabel3 = new OpLabel(330f, 450f, "Zip length.");
            dragger.description = "Only affects Impulse. Default is 140.";

            OpCheckBox check4 = new OpCheckBox(Zip_Through_Walls, new Vector2(300, 420));
            check4.description = "Zipping can displace a slugcat through walls, provided there is space on the other side.";
            OpLabel checklabel4 = new OpLabel(330f, 420f, "Zip through walls.");


            Tabs[0].AddItems(new UIelement[]
            {
                Label1,
                listbox,
                cheatenable,
                Label2,
                check1,
                checklabel1,
                check2,
                checklabel2,
                dragger,
                checklabel3,
                check4,
                checklabel4,
                cheatcoverrect

            });
        }
    }
}
