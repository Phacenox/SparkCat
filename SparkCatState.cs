﻿using System;
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
        }
        const int input_frame_window = 5;
        public float zipLength;

        public int zipCharges = 2;
        public int zipCooldown = 0;


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

        Player.Grasp[] grasps = new Creature.Grasp[2];
        int fakeEatFood = -1;
        Rock fakeeating;
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
                    if (self.grasps[0] != null && self.grasps[0].grabbed is ElectricRubbish.ElectricRubbish er)
                    {
                        self.eatExternalFoodSourceCounter = 4;
                        self.handOnExternalFoodSource = self.grasps[0].grabbed.bodyChunks[0].pos;
                        fakeEatFood = 4;
                        fakeeating = er;
                    }
                    else if (self.grasps[1] != null && self.grasps[1].grabbed is ElectricRubbish.ElectricRubbish er2)
                    {
                        self.eatExternalFoodSourceCounter = 4;
                        self.handOnExternalFoodSource = self.grasps[1].grabbed.bodyChunks[0].pos;
                        fakeEatFood = 4;
                        fakeeating = er2;
                    }
                }
            }else if(tryInteractHold < 0)
            {
                tryInteractHold++;
            }
        }

        public void Update()
        {
            fakeEatFood--;
            if (fakeEatFood == 0 && fakeeating != null && fakeeating is ElectricRubbish.ElectricRubbish er)
            {
                if (er.rubbishAbstract.electricCharge > 0)
                {
                    er.rubbishAbstract.electricCharge = 0;
                    var charged = rechargeZips(2);
                    for (int i = 0; i < 4 - charged; i++)
                        player.AddQuarterFood();
                }
                else if (Math.Max(player.playerState.quarterFoodPoints, player.playerState.foodInStomach * 4) >= 4)
                {
                    player.SubtractFood(1);
                    if (zipCharges == 2)
                    {
                        player.AddQuarterFood();
                        zipCharges = 1;
                    }
                    er.rubbishAbstract.electricCharge = 1;
                    er.room.AddObject(new ZapCoil.ZapFlash(er.firstChunk.pos, 1f));
                    er.room.PlaySound(SoundID.Zapper_Zap, er.firstChunk.pos, .3f, 1.5f + UnityEngine.Random.value * 1.5f);
                    if (er.Submersion > 0.5f)
                    {
                        er.room.AddObject(new UnderwaterShock(er.room, null, er.firstChunk.pos, 10, 800f, 2f, player, new Color(0.8f, 0.8f, 1f)));
                    }
                    er.Spark();
                }
                player.SubtractFood(1);
            }
        }

        public void Zip(InputPackage direction)
        {
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

        public int rechargeZips(int max_available)
        {
            if (max_available == 0) return 0;

            var ret = 2 - zipCharges;
            zipCharges = 2;

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
        int recharge_timer = 60;
        public void ClassMechanicsSparkCat(float zipLength)
        {
            this.zipLength = zipLength;
            //assumes this means inside of iterator
            if (player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.1f)
                recharge_timer--;
            if (recharge_timer <= 0 && zipCharges < 2)
            {
                recharge_timer = 60;
                zipCharges++;
            }else if (zipCharges == 2)
            {
                recharge_timer = 60;
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
                int available_recharges = Math.Max(player.playerState.quarterFoodPoints, player.playerState.foodInStomach * 4);

                if (zipCharges < 2 && available_recharges > 0)
                {
                    int consumed_food = rechargeZips(available_recharges);

                    for (int i = 0; i < 4 - consumed_food; i++)
                    {
                        player.AddQuarterFood();
                    }
                    player.SubtractFood(1);
                }
                else
                {
                    player.room.PlaySound(Sounds.NoDischarge, player.mainBodyChunk.pos, 0.2f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
                    player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));

                    Vector2 vector = Custom.RNV();
                    player.room.AddObject(new Spark(player.firstChunk.pos + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 6));
                }
            }
            else if (zipCooldown == 0 && desires_sparkjump && zipCharges > 0 && !flag2 && (player.input[0].y >= 0 || (player.input[0].y < 0 &&  player.Consious && player.bodyMode != BodyModeIndex.ClimbIntoShortCut && player.onBack == null)))
            {
                zipCooldown = 5;
                Zip(player.input[0]);
            }else if (zipCooldown == 0 && desires_sparkjump)
            {
                zipCooldown = 5;
                player.room.PlaySound(Sounds.NoDischarge, player.mainBodyChunk.pos, 0.3f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
                player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 800f, player, 1f));
                Vector2 vector = Custom.RNV();
                player.room.AddObject(new Spark(player.firstChunk.pos + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 6));
            }
        }
    }
}


