using HUD;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using SlugBase.SaveData;
using SlugBase.Features;
using IL.Menu;

namespace SparkCat
{

    public static class Oracle
    {
        struct ConversationMacro
        {
            public Conversation self;
            public ConversationMacro(Conversation self) { this.self = self; }
            public void Say(string text, int linger = 0)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, text, linger));
            }
            public void Wait(int physFrames)
            {
                if (self is SSOracleBehavior.PebblesConversation peebb)
                    self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, peebb.convBehav, physFrames));
                else
                    self.events.Add(new Conversation.WaitEvent(self, physFrames));
            }
        }
        public static void PebblesConversationAddEventsHook(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            if (self.owner.oracle.room.game.StoryCharacter.value != Plugin.SLUGCAT_NAME)
            {
                orig(self);
                return;
            }
            var macro = new ConversationMacro(self);

            if (!self.owner.playerEnteredWithMark)
            {
                macro.Say("Figures.", 20);
                macro.Say("Returning to me once again, and somehow losing your mark... You are surely committed to being as annoying as possible.");
            }
            else
            {
                macro.Wait(10);
                macro.Say("...");
                macro.Wait(20);
                macro.Say("And so it returns. Of course...", 40);
                macro.Say("Why do you insist on pestering me? I am far too busy for this nonsense.", 20);
            }
            macro.Wait(120);
            macro.Say("Hear me well, creature.", 20);
            macro.Say("Think of all that I have gifted you...", 15);
            macro.Say("Knowledge, food, shelter, the privilege to explore even the deepest reaches of my can...", 15);
            macro.Say("Despite these gifts you continue to return here to my chamber; the only room in which you are forbidden.", 15);
            macro.Say("You should understand by now that my work requires my full concentration... I can no longer stomach your interruptions.", 30);
            macro.Say("Should you return to me again, I will not be so generous. But this time, all I will do is say it clearly:", 30);
            macro.Say("Get out. Do not come back.", 10);
        }

        public static void SSSleepoverBehaviorctorHook(On.SSOracleBehavior.SSSleepoverBehavior.orig_ctor orig, SSOracleBehavior.SSSleepoverBehavior self, SSOracleBehavior owner)
        {
            orig(self, owner);
            if (self.oracle.room.game.StoryCharacter.value != Plugin.SLUGCAT_NAME)
                return;

            self.timeUntilNextPanic = int.MaxValue;
            self.lowGravity = -1f;
            if (self.owner.conversation != null)
            {
                self.owner.conversation.Destroy();
                self.owner.conversation = null;
                return;
            }
            self.owner.TurnOffSSMusic(true);
            owner.getToWorking = 1f;
            self.gravOn = true;

            if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad / 100 == 0)
            {
                self.dialogBox.Interrupt("Oh, its you! ...Did Pebbles kick you out again?", 40);
                self.dialogBox.NewMessage("I can see it's in your nature, but it would probably be best if you stopped going back to him.", 10);
                self.dialogBox.NewMessage("Lately he's been very... distracted. I'm worried that he might do something to you if you continue bothering him.", 15);
                self.dialogBox.NewMessage("I don't know what it is that drives you to hop between us so often. Please, for your sake, you should find a different iterator to visit.", 10);
                self.dialogBox.NewMessage("That being said, you're welcome to stay with me for as long as you like. Just don't go eating too many of my neurons, okay?", 10);
            }
            else if (Random.value < 0.3f)
            {
                self.dialogBox.Interrupt("Welcome back!", 10);
            }
            else if (Random.value < 0.3f)
            {
                self.dialogBox.Interrupt("Hello.", 10);
            }
            else
            {
                self.dialogBox.Interrupt("Oh, hello again!", 10);
            }
            self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad += 100;
        }


        public static void OracleSeePlayerHook(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            if (self.oracle.room.game.StoryCharacter.value != Plugin.SLUGCAT_NAME)
            {
                orig(self);
                return;
            }
            if(self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
            {
                self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty);
                return;
            }
            else
            {
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad % 100 == 0)
                {
                    self.pearlPickupReaction = false;
                    self.NewAction(SSOracleBehavior.Action.MeetWhite_Shocked);
                    self.SlugcatEnterRoomReaction();
                    self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad += 1;
                    return;
                }
                else
                {
                    self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                    return;
                }
            }
        }

        internal static void SSUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (self.oracle.room.game.StoryCharacter.value != Plugin.SLUGCAT_NAME)
                return;
            if (self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut)
            {
                if (self.throwOutCounter == 700)
                    self.dialogBox.Interrupt("Now.", 0);
                else if(self.throwOutCounter == 1530)
                    self.dialogBox.Interrupt("Final warning. OUT.", 0);

            }
        }


        /* moon's only interest is pearls in sm's timeline
        public static void MoonConversationAddEventsHook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            if(self.currentSaveFile.value != Plugin.SLUGCAT_NAME)
            {
                orig(self);
                return;
            }
            var macro = new ConversationMacro(self);
            if(self.id == Conversation.ID.Moon_Misc_Item)
            {
                if(self.describeItem == SLOracleBehaviorHasMark.MiscItemType.Rock)
                {
                    macro.Say("It's a rock. Thank you, I guess...");
                    macro.Say("Although I suppose it could be useful for you... During electrical storms, I've observed fragments of debris holding an electrical charge for an extended period of time.");
                    macro.Say("This one seems more than capable.");
                    //self.events.Add(new Conversation.TextEvent(self, 10, "Oh my, this is holding a dangerous amount of electricity! ...For someone of your size, at least. Do be careful with it.", 0));
                    return;
                }
            }
            orig(self);
        }*/
    }
}
