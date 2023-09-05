using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkCat
{
    public class SparkCatOptions: OptionInterface
    {
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

            Tabs[0].AddItems(new UIelement[]
            {
                Label1,
                listbox
            });
        }
    }
}
