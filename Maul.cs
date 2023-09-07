using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace SparkCat
{
    internal class Maul
    {
        public static void GrabBehavior(Player self, bool eu, SparkCatState state)
        {

            //electric maul
            if (self.maulTimer == 39 && self.input[0].pckp)
            {
                var maul_grasp_index = 0;
                if ((self.grasps[0] == null || !(self.grasps[0].grabbed is Creature)) && self.grasps[1] != null && self.grasps[1].grabbed is Creature)
                    maul_grasp_index = 1;
                var maul_grasp = self.grasps[maul_grasp_index];
                if (self.slugOnBack != null)
                {
                    self.slugOnBack.increment = false;
                    self.slugOnBack.interactionLocked = true;
                }
                if (self.spearOnBack != null)
                {
                    self.spearOnBack.increment = false;
                    self.spearOnBack.interactionLocked = true;
                }
                if (maul_grasp != null)
                {
                    if (RainWorld.ShowLogs)
                    {
                        Debug.Log("Impulse: Mauled Target");
                    }
                    if (ChargeablesState.HasEnoughCharge(state, 3))
                    {
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_A, self.mainBodyChunk, false, 0.6f, 1f);
                        self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        if (maul_grasp.grabbed is Creature c && !c.dead)
                        {
                            for (int i = 0; i < 15; i++)
                            {
                                Vector2 vector = Custom.DegToVec(360f * Random.value);
                                self.room.AddObject(new MouseSpark(self.firstChunk.pos + vector * 9f, self.firstChunk.vel + vector * 36f * Random.value, 20f, new Color(0.7f, 1f, 1f)));
                            }

                            Creature creature = maul_grasp.grabbed as Creature;
                            creature.SetKillTag(self.abstractCreature);
                            var isElectric = ElectricRubbish.ElectricRubbish.CheckElectricCreature(creature);
                            if (isElectric)
                            {
                                state.rechargeZipStorage(6);
                                creature.stun = 5;
                            }
                            else
                            {
                                ChargeablesState.SpendCharge(state, 3);
                                self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos, 1f, 1f);
                                self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                                float stunBonus = (!(creature is Player)) ? (320f * Mathf.Lerp(creature.Template.baseStunResistance, 1f, 0.5f)) : 70f;
                                creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), maul_grasp.grabbedChunk, null, Creature.DamageType.Electric, 0f, stunBonus);
                                creature.stun = (int)stunBonus;
                                self.room.AddObject(new CreatureSpasmer(creature, allowDead: false, creature.stun));
                            }
                            if (creature is Inspector ins)
                                ins.anger = 1;
                        }
                    }
                    else
                    {
                        state.DoFailureEffect();
                    }
                    self.maulTimer = 0;
                    self.wantToPickUp = 0;
                    if (maul_grasp != null)
                    {
                        self.TossObject(maul_grasp_index, eu);
                        self.ReleaseGrasp(maul_grasp_index);
                    }
                    self.standing = true;
                }
            }
        }
    }
}
