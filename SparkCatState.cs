using System;
using System.Collections.Generic;
using MoreSlugcats;
using Noise;
using RWCustom;
using UnityEngine;
using static Player;

namespace SparkCat
{
    public  class SparkCatState
    {
        public Player player;
        public SparkCatState(Player player)
        {
            this.player = player;
            stored_charges = max_stored_charges;
        }
        const int input_frame_window = 5;
        public float zipLength;

        public int zipCharges = 2;
        public int zipCooldown = 0;

        public const int max_stored_charges = 12;
        public int stored_charges = 12;

        public bool zipping
        {
            get => zipFrame > 0;
            set => zipFrame = value ? 6 : 0;
        }
        Vector2 startpos;
        Vector2 endpos;
        IntVector2 zipDirection;
        public int zipFrame = 0;
        public bool graphic_teleporting = false;

        Creature.Grasp[] grasps = new Creature.Grasp[2];
        int fakeEatFood = -1;
        Weapon fakeeating;
        public int tryInteractHold = 0;
        public void GrabHook(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (grasps[0] != self.grasps[0] ||  grasps[1] != self.grasps[1] )
                tryInteractHold = -1;
            grasps[0] = self.grasps[0];
            grasps[1] = self.grasps[1];
            if (tryInteractHold == 0 && self.input[0].pckp && !self.input[1].pckp)
                tryInteractHold = 10;
            if (tryInteractHold >= 0 && !self.input[0].pckp || zipCooldown > 0)
                tryInteractHold = 0;

            if(tryInteractHold > 0)
            {
                tryInteractHold--;
                if (tryInteractHold == 0 && zipCooldown == 0)
                {
                    Weapon w = null;
                    foreach(int i in new int[] { 0, 1 })
                    {
                        if (self.grasps[i] != null && self.grasps[i].grabbed is ElectricRubbish.ElectricRubbish er)
                        {
                            w = er;
                            break;
                        }else if (self.grasps[i] != null && self.grasps[i].grabbed is ElectricSpear es)
                        {
                            w = es;
                            break;
                        }
                    }

                    if(w != null){
                        self.eatExternalFoodSourceCounter = 4;
                        self.handOnExternalFoodSource = w.bodyChunks[0].pos;
                        fakeEatFood = 4;
                        fakeeating = w;
                    }
                }
            }else if(tryInteractHold < 0)
            {
                tryInteractHold++;
            }
        }
        int ChargeOf(Weapon w)
        {
            if (w is ElectricRubbish.ElectricRubbish er)
                return er.rubbishAbstract.electricCharge;
            if (w is ElectricSpear es)
                return es.abstractSpear.electricCharge;
            return 0;
        }
        void SetCharge(Weapon w, int charge)
        {
            if (w is ElectricRubbish.ElectricRubbish er)
                er.rubbishAbstract.electricCharge = charge;
            if (w is ElectricSpear es)
                es.abstractSpear.electricCharge = charge == 1 ? 3: 0;
        }

        const int foodvalue = 6;
        public void Update()
        {
            fakeEatFood--;
            if (fakeEatFood == 0 && fakeeating != null)
            {
                int cost = fakeeating is ElectricSpear ? 12 : 6;
                if (fakeeating is ElectricRubbish.ElectricRubbish && ChargeOf(fakeeating) > 0)
                {
                    if(rechargeZipStorage(cost) > 0)
                        SetCharge(fakeeating, 0);
                    else
                        DoFailureEffect();
                }
                else if(ChargeOf(fakeeating) == 0)
                {
                    if (stored_charges + player.FoodInStomach * foodvalue >= cost)
                    {
                        while (cost > stored_charges)
                        {
                            cost -= foodvalue;
                            player.SubtractFood(1);
                            if (cost < 0)
                            {
                                stored_charges -= cost;
                                cost = 0;
                            }
                        }
                        stored_charges -= cost;

                        SetCharge(fakeeating, 1);

                        player.room.AddObject(new ZapCoil.ZapFlash(fakeeating.firstChunk.pos, 0.5f));
                        player.room.PlaySound(SoundID.Zapper_Zap, fakeeating.firstChunk.pos, .3f, 1.5f + UnityEngine.Random.value * 1.5f);
                        player.room.PlaySound(SoundID.Rock_Hit_Creature, fakeeating.firstChunk, false, 0.6f, 1);
                        if (fakeeating.Submersion > 0.5f)
                            fakeeating.room.AddObject(new UnderwaterShock(fakeeating.room, null, fakeeating.firstChunk.pos, 10, 800f, 2f, player, new Color(0.8f, 0.8f, 1f)));

                    }
                    else
                        DoFailureEffect();
                }
                //the eatExternalFoodSourceCounter animation ends with a food increase. counteract this.
                player.playerState.foodInStomach--;
            }
        }

        public void Zip(InputPackage direction)
        {
            grounded_since_last_zip = false;
            zipDirection = direction.IntVec;
            if (player.wantToJump > 0) player.wantToJump = 0;
            zipCharges--;
            startpos = player.firstChunk.pos;
            endpos = startpos + zipDirection.ToVector2().normalized * zipLength;

            IntVector2 tilestart = player.room.GetTilePosition(player.firstChunk.pos);
            IntVector2 tileend = player.room.GetTilePosition(endpos);
            List<IntVector2> tiles = new List<IntVector2>();
            player.room.RayTraceTilesList(tilestart.x, tilestart.y, tileend.x, tileend.y, ref tiles);
            for (int i = 1; i < tiles.Count; i++)
            {
                if (player.room.GetTile(tiles[i]).Solid)
                {
                    endpos = player.room.MiddleOfTile(tiles[i - 1]);
                    break;
                }
            }

            zipping = true;
            MakeZipEffect(startpos, 6, 1f, player);
            MakeZipEffect(endpos, 3, 0.6f);
            player.room.PlaySound(Sounds.QuickZap, endpos, 0.3f + UnityEngine.Random.value * 0.1f, 0.8f + UnityEngine.Random.value * 1.7f);
            player.room.InGameNoise(new InGameNoise(endpos, 800f, player, 1f));
        }

        public int rechargeZipStorage(int max_available)
        {
            if (max_available == 0 || max_stored_charges == stored_charges) return 0;

            var ret = max_stored_charges - stored_charges;
            ret = Mathf.Min(ret, max_available);
            stored_charges += ret;

            MakeZipEffect(player.firstChunk.pos, 3, 0.6f, player);
            player.room.PlaySound(Sounds.Recharge, player.mainBodyChunk.pos, 0.3f + UnityEngine.Random.value * 0.1f, 0.8f + UnityEngine.Random.value * 0.5f);
            player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));
            return ret;
        }

        public void DoZip()
        {
            graphic_teleporting = false;
            zipFrame--;
            if(zipFrame == 1)
                player.room.AddObject(new ZipSwishEffect(player.firstChunk.pos, endpos, 5.5f, 0.4f, Color.white));

            if(zipFrame == 0)
            {
                graphic_teleporting = true;
                startpos = player.firstChunk.pos;
                if (zipDirection == new IntVector2(0,0))
                    endpos = startpos + Vector2.up * 3;
                MakeZipEffect(startpos, 3, 0.6f);
                MakeZipEffect(endpos, 6, 1f, player);
                var distance = endpos -  startpos;

                if (player.slugOnBack != null && player.slugOnBack.HasASlug)
                    ObjectTeleports.TrySmoothTeleportObject(player.slugOnBack.slugcat, distance);
                if(player.spearOnBack != null)
                    ObjectTeleports.TrySmoothTeleportObject(player.spearOnBack.spear, distance);
                ObjectTeleports.TrySmoothTeleportObject(player, distance);
                foreach (var i in player.grasps)
                    if(i != null)
                        ObjectTeleports.TrySmoothTeleportObject(i.grabbed, distance);

                var target_vel = (endpos - startpos).normalized * 3;
                if (Mathf.Abs(target_vel.y) < 0.7f)
                    target_vel.y = 0.1f * Mathf.Sign(player.bodyChunks[0].vel.y);
                if (player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.1f)
                    target_vel = zipDirection.ToVector2().normalized * 4;

                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    var old_vel = player.bodyChunks[i].vel;
                    //no slowing down unless intent
                    if (Mathf.Sign(old_vel.x) == Mathf.Sign(target_vel.x) && !(Mathf.Abs(target_vel.x) < 0.01f))
                        player.bodyChunks[i].vel.x = Mathf.Sign(old_vel.x) * Mathf.Max(Mathf.Abs(old_vel.x), Mathf.Abs(target_vel.x));
                    else
                        player.bodyChunks[i].vel.x = target_vel.x;

                    if (Mathf.Sign(old_vel.y) == Mathf.Sign(target_vel.y) && !(Mathf.Abs(target_vel.y) < 0.01f))
                        player.bodyChunks[i].vel.y = Mathf.Sign(old_vel.y) * Mathf.Max(Mathf.Abs(old_vel.y), Mathf.Abs(target_vel.y));
                    else
                        player.bodyChunks[i].vel.y = target_vel.y;

                }
            }

            if(zipFrame <= 0 && zipFrame > -5)
            {
                //if not zero G, Y velocity is at least 1
                if (!(player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.1f))
                {
                    player.bodyChunks[0].vel.y = Mathf.Max(player.bodyChunks[0].vel.y, 0f);
                    player.bodyChunks[1].vel.y = Mathf.Max(player.bodyChunks[1].vel.y, 0f);
                    player.customPlayerGravity = 0f;
                    player.SetLocalAirFriction(0.7f);
                }
            }
        }
        public void MakeZipEffect(Vector2 where, float size,float alpha, Player follow = null)
        {
            player.room.AddObject(new ZipFlashEffect(where, size, alpha, 3, Color.white, follow));
            for (int j = 0; j < 10; j++)
            {
                Vector2 vector = Custom.RNV();
                player.room.AddObject(new Spark(where + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 8));
            }
        }
        (bool, bool)[] recent_inputs = new (bool, bool)[input_frame_window];
        int recharge_timer = 40;
        bool grounded_since_last_zip = false;

        int iterator_recharge = 30;
        public void ClassMechanicsSparkCat(float zipLength)
        {
            this.zipLength = zipLength;
            if (player.canJump > 0)
                grounded_since_last_zip = true;
            if(player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.2f && zipFrame < -5)
            {
                //assume encapsulating check means inside iterator. TODO?: make more specific
                iterator_recharge--;
                recharge_timer--;
            }
            else if (grounded_since_last_zip)
            {
                recharge_timer--;
            }
            if(iterator_recharge <= 0 && stored_charges < max_stored_charges)
            {
                stored_charges++;
                iterator_recharge = 30;
            }
            if (stored_charges > 0 && recharge_timer <= 0 && zipCharges < 2)
            {
                recharge_timer = 40;
                stored_charges--;
                zipCharges++;
            }else if (zipCharges == 2)
            {
                recharge_timer = 40;
            }

            (bool, bool) new_inputs = (player.input[0].jmp, player.input[0].pckp);
            bool desires_sparkjump = new_inputs.Item1 && new_inputs.Item2 && (!recent_inputs[0].Item1 || !recent_inputs[0].Item2) && (!recent_inputs[recent_inputs.Length-1].Item1 && !recent_inputs[recent_inputs.Length - 1].Item2);
            for (int i = 1; i < recent_inputs.Length; i++)
            {
                recent_inputs[i] = recent_inputs[i - 1];
            }
            recent_inputs[0] = new_inputs;

            if(zipCooldown > 0)
                zipCooldown--;

            bool flag2 = player.eatMeat >= 20 || player.maulTimer >= 15;
            if (zipping) return;

            if (zipCooldown == 0 && desires_sparkjump && (player.canJump > 0 || player.bodyMode == BodyModeIndex.CorridorClimb)
                && !player.submerged && !flag2
                && ((player.input[0].y < 0 && player.bodyMode != BodyModeIndex.CorridorClimb && player.bodyMode != BodyModeIndex.ClimbingOnBeam)
                    || (player.bodyMode == BodyModeIndex.Crawl || player.bodyMode == BodyModeIndex.CorridorClimb || player.bodyMode == BodyModeIndex.ClimbingOnBeam) && player.input[0].x == 0 && player.input[0].y == 0)
                && player.Consious)
            {
                zipCooldown = 5;

                if (player.playerState.foodInStomach > 0 && rechargeZipStorage(6) > 0)//short circuit
                {
                    player.SubtractFood(1);
                }
                else
                {
                    DoFailureEffect();
                }
            }
            else if (zipCooldown == 0 && desires_sparkjump && zipCharges > 0 && !flag2 && (player.input[0].y >= 0 || (player.input[0].y < 0 &&  player.Consious && player.bodyMode != BodyModeIndex.ClimbIntoShortCut && player.onBack == null)))
            {
                zipCooldown = 5;
                Zip(player.input[0]);
            }else if (zipCooldown == 0 && desires_sparkjump)
            {
                zipCooldown = 5;
                DoFailureEffect();
            }
        }
        void DoFailureEffect()
        {

            player.room.PlaySound(Sounds.NoDischarge, player.mainBodyChunk.pos, 0.2f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
            player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));
            Vector2 vector = Custom.RNV();
            player.room.AddObject(new Spark(player.firstChunk.pos + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 6));
        }
    }
}


