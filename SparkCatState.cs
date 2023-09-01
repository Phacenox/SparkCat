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
        public SparkCatGraphics graphics;
        public SparkCatState(Player player, PlayerGraphics graphics)
        {
            this.player = player;
            this.graphics = new SparkCatGraphics(graphics, this);
        }
        const int input_frame_window = 5;
        public float zipLength;

        public int zipCharges = 2;
        public float zipCooldown = 0f;


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

        public void Zip(InputPackage direction)
        {
            zipDirection = direction.IntVec;
            if (player.wantToJump > 0) player.wantToJump = 0;
            zipCharges--;
            startpos = player.firstChunk.pos;
            endpos = startpos + zipDirection.ToVector2().normalized * zipLength;
            zipping = true;
            MakeZipEffect(startpos, 6, 1f, player);
            MakeZipEffect(endpos, 3, 0.6f);
            player.room.PlaySound(Sounds.QuickZap, endpos, 0.3f + UnityEngine.Random.value * 0.1f, 0.5f + UnityEngine.Random.value * 2f);
            player.room.InGameNoise(new InGameNoise(endpos, 800f, player, 1f));
        }
        public void DoZip()
        {
            graphic_teleporting = false;
            zipFrame--;
            if(zipFrame == 1)
            {
                player.room.AddObject(new ZipSwishEffect(player.firstChunk.pos, endpos, 5.5f, 0.4f, Color.white));
            }
            if(zipFrame == 0)
            {
                graphic_teleporting = true;
                startpos = player.firstChunk.pos;
                if (zipDirection == new IntVector2(0,0))
                    endpos = startpos + Vector2.up * 3;
                MakeZipEffect(startpos, 3, 0.6f);
                MakeZipEffect(endpos, 6, 1f, player);
                var distance = endpos -  startpos;
                graphics.TeleportTail(distance);

                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    player.bodyChunks[i].HardSetPosition(endpos + (player.bodyChunks[i].pos - startpos));
                }
                var target_vel = (endpos - startpos).normalized * 3;
                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    var old_vel = player.bodyChunks[i].vel;
                    //no slowing down unless intent
                    if (Mathf.Sign(old_vel.x) == Mathf.Sign(target_vel.x))
                    {
                        player.bodyChunks[i].vel.x = target_vel.x;
                        player.bodyChunks[i].vel.x = Mathf.Sign(old_vel.x) * Mathf.Max(Mathf.Abs(old_vel.x), Mathf.Abs(target_vel.x));
                    }
                    else

                    if (Mathf.Sign(old_vel.y) == Mathf.Sign(target_vel.y))
                    {
                        player.bodyChunks[i].vel.y = Mathf.Sign(old_vel.y) * Mathf.Max(Mathf.Abs(old_vel.y), Mathf.Abs(target_vel.y));
                    }
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
        int recharge_timer = 90;
        public void ClassMechanicsSparkCat(float strength)
        {
            recharge_timer--;
            if (recharge_timer <= 0 && zipCharges < 2)
            {
                recharge_timer = 70;
                zipCharges++;
            }else if (zipCharges == 2)
            {
                recharge_timer = 70;
            }

            zipLength = strength;
            (bool, bool) new_inputs = (player.input[0].jmp, player.input[0].pckp);
            bool desires_sparkjump = new_inputs.Item1 && new_inputs.Item2 && (!recent_inputs[0].Item1 || !recent_inputs[0].Item2) && (!recent_inputs[recent_inputs.Length-1].Item1 && !recent_inputs[recent_inputs.Length - 1].Item2);
            for (int i = 1; i < recent_inputs.Length; i++)
            {
                recent_inputs[i] = recent_inputs[i - 1];
            }
            recent_inputs[0] = new_inputs;


            zipCooldown--;

            bool flag2 = player.eatMeat >= 20 || player.maulTimer >= 15;
            if (zipping) return;

            if (desires_sparkjump && (player.canJump > 0 || player.bodyMode == BodyModeIndex.CorridorClimb)
                && !player.submerged && !flag2
                && ((player.input[0].y < 0 && player.bodyMode != BodyModeIndex.CorridorClimb && player.bodyMode != BodyModeIndex.ClimbingOnBeam)
                    || (player.bodyMode == BodyModeIndex.Crawl || player.bodyMode == BodyModeIndex.CorridorClimb || player.bodyMode == BodyModeIndex.ClimbingOnBeam) && player.input[0].x == 0 && player.input[0].y == 0)
                && player.Consious)
            {
                
                if (zipCharges < 2)
                {
                    player.playerState.quarterFoodPoints -= 2 - zipCharges;
                    zipCharges = 2;
                    MakeZipEffect(player.firstChunk.pos, 3, 0.6f, player);
                    player.room.PlaySound(Sounds.Recharge, player.mainBodyChunk.pos, 0.3f + UnityEngine.Random.value * 0.1f, 0.8f + UnityEngine.Random.value * 0.5f);
                    player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));
                    zipCooldown = 5f;
                }
                else
                {
                    Debug.Log(1);
                    Debug.Log(UnityEngine.Random.value);
                    player.room.PlaySound(Sounds.NoDischarge, player.mainBodyChunk.pos, 0.2f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
                    Debug.Log(2);
                    player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));

                    Vector2 vector = Custom.RNV();
                    player.room.AddObject(new Spark(player.firstChunk.pos + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 6));
                }
            }
            else if (desires_sparkjump && zipCharges > 0 && !flag2 && (player.input[0].y >= 0 || (player.input[0].y < 0 &&  player.Consious && player.bodyMode != BodyModeIndex.ClimbIntoShortCut && player.onBack == null)))
            {
                zipCooldown = 5f;
                Zip(player.input[0]);
            }else if (desires_sparkjump)
            {
                player.room.PlaySound(Sounds.NoDischarge, player.mainBodyChunk.pos, 0.3f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
                player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 800f, player, 1f));
                Vector2 vector = Custom.RNV();
                player.room.AddObject(new Spark(player.firstChunk.pos + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 6));
            }
        }
    }
}


