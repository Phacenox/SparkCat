namespace SparkCat
{
    internal static class Crafting
    {

        public static bool CanBeCraftedHook(On.Player.orig_GraspsCanBeCrafted orig, Player self)
        {
            if (Plugin.SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                return self.input[0].y == 1 && self.CraftingResults() == AbstractPhysicalObject.AbstractObjectType.Spear;
            }
            return orig(self);
        }

        public static AbstractPhysicalObject.AbstractObjectType CraftingResultHook(On.Player.orig_CraftingResults orig, Player self)
        {
            if (Plugin.SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                if (self.grasps[0] != null && self.grasps[1] != null)
                {
                    if (self.grasps[0].grabbed is Spear s && !s.abstractSpear.electric && self.grasps[1].grabbed is ElectricRubbish.ElectricRubbish er && er.rubbishAbstract.electricCharge > 0)
                    {
                        Plugin.states[self].chargeablesState.tryInteractHold = -1;
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }
                    if (self.grasps[1].grabbed is Spear s2 && !s2.abstractSpear.electric && self.grasps[0].grabbed is ElectricRubbish.ElectricRubbish er2 && er2.rubbishAbstract.electricCharge > 0)
                    {
                        Plugin.states[self].chargeablesState.tryInteractHold = -1;
                        return AbstractPhysicalObject.AbstractObjectType.Spear;
                    }
                }
            }
            return orig(self);
        }

        public static void SpitUpCraftedHook(On.Player.orig_SpitUpCraftedObject orig, Player self)
        {
            if (Plugin.SparkJump.TryGet(self, out float jumpStrength) && jumpStrength > 0)
            {
                var state = Plugin.states[self];

                int cost = ChargeablesState.spearChargeValue - ChargeablesState.rubbishChargeValue;
                if (!ChargeablesState.HasEnoughCharge(state, cost))
                {
                    state.DoFailureEffect();
                    return;
                }
                ChargeablesState.SpendCharge(state, cost);

                self.room.PlaySound(Enums.NoDischarge, self.mainBodyChunk.pos, 0.2f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
                self.room.PlaySound(SoundID.Rock_Hit_Creature, self.mainBodyChunk, false, 0.4f, 1);
                if (self.grasps[0] == null || self.grasps[1] == null)
                    return;
                var phys1 = self.grasps[0].grabbed.abstractPhysicalObject;
                var phys2 = self.grasps[1].grabbed.abstractPhysicalObject;
                self.ReleaseGrasp(0);
                self.ReleaseGrasp(1);
                phys1.realizedObject.RemoveFromRoom();
                phys2.realizedObject.RemoveFromRoom();
                self.room.abstractRoom.RemoveEntity(phys1);
                self.room.abstractRoom.RemoveEntity(phys2);
                AbstractSpear newSpear = new AbstractSpear(self.room.world, null, self.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                self.room.abstractRoom.AddEntity(newSpear);
                newSpear.RealizeInRoom();
                if (self.FreeHand() != -1)
                    self.SlugcatGrab(newSpear.realizedObject, self.FreeHand());
                return;
            }
            else
            {
                orig(self);
            }
        }
    }
}
