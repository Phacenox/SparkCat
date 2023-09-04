using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SparkCat
{
    public class ChargeablesState
    {
        SparkCatState state;
        public ChargeablesState(SparkCatState state)
        {
            this.state = state;
        }

        Creature.Grasp[] grasps = new Creature.Grasp[2];
        int chargeHeldItem = -1;
        PhysicalObject chargeTarget;
        int chargeGrasp;
        public int tryInteractHold = 0;
        public void GrabHook(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (grasps[0] != self.grasps[0] || grasps[1] != self.grasps[1])
                tryInteractHold = -1;
            grasps[0] = self.grasps[0];
            grasps[1] = self.grasps[1];
            if (tryInteractHold == 0 && self.input[0].pckp && !self.input[1].pckp)
                tryInteractHold = 10;
            if (tryInteractHold >= 0 && !self.input[0].pckp || state.zipCooldown > 0)
                tryInteractHold = 0;

            if (tryInteractHold > 0)
            {
                tryInteractHold--;
                if (tryInteractHold == 0 && state.zipCooldown == 0 && chargeHeldItem < 0)
                {
                    for(int i = 0; i < self.grasps.Length; i++)
                    {
                        if (self.grasps[i] != null && (self.grasps[i].grabbed is ElectricRubbish.ElectricRubbish || self.grasps[i].grabbed is ElectricSpear || self.grasps[i].grabbed is Rock))
                        {
                            chargeTarget = self.grasps[i].grabbed;
                            self.eatExternalFoodSourceCounter = 4;
                            self.handOnExternalFoodSource = chargeTarget.bodyChunks[0].pos;
                            chargeHeldItem = 4;
                            chargeGrasp = i;
                            break;
                        }
                    }
                }
            }
            else if (tryInteractHold < 0)
            {
                tryInteractHold++;
            }
        }
        const int foodvalue = 6;
        public void Update()
        {
            chargeHeldItem--;
            if (chargeHeldItem == 0 && chargeTarget != null)
            {
                int cost = chargeTarget is ElectricSpear ? 12 : 6;
                if (chargeTarget is ElectricRubbish.ElectricRubbish && ChargeOf(chargeTarget) > 0)
                {
                    if (state.rechargeZipStorage(cost) > 0)
                        SetCharge(chargeTarget, 0);
                    else
                        state.DoFailureEffect();
                }
                else if (ChargeOf(chargeTarget) == 0)
                {
                    if (state.zipChargesStored + state.player.FoodInStomach * foodvalue >= cost)
                    {
                        while (cost > state.zipChargesStored)
                        {
                            cost -= foodvalue;
                            state.player.SubtractFood(1);
                            if (cost < 0)
                            {
                                state.zipChargesStored -= cost;
                                cost = 0;
                            }
                        }
                        state.zipChargesStored -= cost;

                        SetCharge(chargeTarget, 1);

                        state.player.room.AddObject(new ZapCoil.ZapFlash(chargeTarget.firstChunk.pos, 0.5f));
                        state.player.room.PlaySound(SoundID.Zapper_Zap, chargeTarget.firstChunk.pos, .3f, 1.5f + UnityEngine.Random.value * 1.5f);
                        state.player.room.PlaySound(SoundID.Rock_Hit_Creature, chargeTarget.firstChunk, false, 0.6f, 1);
                        if (chargeTarget.Submersion > 0.5f)
                            chargeTarget.room.AddObject(new UnderwaterShock(chargeTarget.room, null, chargeTarget.firstChunk.pos, 10, 800f, 2f, state.player, new Color(0.8f, 0.8f, 1f)));

                    }
                    else
                        state.DoFailureEffect();
                }
                //the eatExternalFoodSourceCounter animation ends with a food increase. counteract this.
                state.player.playerState.foodInStomach--;
            }
        }

        int ChargeOf(PhysicalObject w)
        {
            if (w is ElectricRubbish.ElectricRubbish er)
                return er.rubbishAbstract.electricCharge;
            if (w is ElectricSpear es)
                return es.abstractSpear.electricCharge;
            if (w is Rock)
                return 0;
            return 0;
        }
        void SetCharge(PhysicalObject w, int charge)
        {
            if (w is ElectricRubbish.ElectricRubbish er)
                er.rubbishAbstract.electricCharge = charge;
            else if (w is ElectricSpear es)
                es.abstractSpear.electricCharge = charge == 1 ? 3 : 0;
            else if (w is Rock && charge == 1)
            {
                var abst = w.abstractPhysicalObject;
                state.player.ReleaseGrasp(chargeGrasp);
                w.RemoveFromRoom();
                state.player.room.abstractRoom.RemoveEntity(abst);
                ElectricRubbish.ElectricRubbishAbstract era = new ElectricRubbish.ElectricRubbishAbstract(state.player.room.world, state.player.coord, state.player.room.game.GetNewID(), 1);
                state.player.room.abstractRoom.AddEntity(era);
                era.RealizeInRoom();
                if(state.player.FreeHand() != -1)
                    state.player.SlugcatGrab(era.realizedObject, state.player.FreeHand());
            }
        }
    }
}
