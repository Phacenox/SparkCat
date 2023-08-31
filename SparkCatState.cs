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
        const int input_frame_window = 5;
        public SparkCatState(Player player)
        {
            this.player = player;
        }
        Player player;
        public float jumpstrength;

        public int sparkJumps = 2;
        public float sparkJumpCooldown = 0f;


        public bool sparkJumping
        {
            get => sparkJumpFrame > 0;
            set => sparkJumpFrame = value ? 3 : 0;
        }
        Vector2 startpos;
        Vector2 endpos;
        public int sparkJumpFrame = 0;

        public void SparkJump(Player.InputPackage direction)
        {
            //Debug.Log(player.customPlayerGravity);
            //player.customPlayerGravity -= 1;
            if (player.wantToJump > 0) player.wantToJump = 0;
            player.jumpBoost = 0;
            sparkJumps--;
            startpos = player.firstChunk.pos;
            endpos = startpos + direction.IntVec.ToVector2().normalized * jumpstrength;
            sparkJumping = true;
            MakeSparkJumpEffect(startpos);
            MakeSparkJumpEffect(endpos);
            player.room.PlaySound(SoundID.Fire_Spear_Explode, startpos, 0.3f + UnityEngine.Random.value * 0.3f, 0.5f + UnityEngine.Random.value * 2f);
            player.room.InGameNoise(new InGameNoise(endpos, 800f, player, 1f));
        }
        public void DoSparkJump()
        {
            sparkJumpFrame--;
            if(sparkJumpFrame == 0)
            {
                for(int i = 0; i < player.bodyChunks.Length; i++)
                    player.bodyChunks[i].HardSetPosition(endpos + (player.bodyChunks[i].pos - startpos));
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
                //if not zero G, Y velocity is at least 1
                if (!(player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.1f))
                {
                    player.bodyChunks[0].vel.y = Mathf.Max(player.bodyChunks[0].vel.y, 1);
                    player.bodyChunks[1].vel.y = Mathf.Max(player.bodyChunks[0].vel.y, 1);
                }
                player.jumpBoost = 0;
                //player.customPlayerGravity += 1;
            }
        }
        public void MakeSparkJumpEffect(Vector2 where)
        {
            for (int i = 0; i < 4; i++)
            {
                player.room.AddObject(new Explosion.ExplosionSmoke(where, Custom.RNV() * 5f * UnityEngine.Random.value, 1f));
            }

            player.room.AddObject(new Explosion.ExplosionLight(where, 80f, 1f, 2, Color.white));
            for (int j = 0; j < 10; j++)
            {
                Vector2 vector = Custom.RNV();
                player.room.AddObject(new Spark(where + vector * UnityEngine.Random.value * 10f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 8));
            }
        }
        (bool, bool)[] recent_inputs = new (bool, bool)[input_frame_window];
        int recharge_timer = 90;
        public void ClassMechanicsSparkCat(float strength)
        {
            if(sparkJumps >= 2)
                player.room.AddObject(new Spark(player.firstChunk.pos + Vector2.right * 4f, Vector2.right * 10f, Color.white, null, 4, 6));
            if (sparkJumps >= 1)
                player.room.AddObject(new Spark(player.firstChunk.pos + Vector2.left * 4f, Vector2.left * 10f, Color.white, null, 4, 6));
            recharge_timer--;
            if (recharge_timer <= 0 && sparkJumps < 2)
            {
                recharge_timer = 70;
                sparkJumps++;
            }else if (sparkJumps == 2)
            {
                recharge_timer = 70;
            }

            jumpstrength = strength;
            (bool, bool) new_inputs = (player.input[0].jmp, player.input[0].pckp);
            bool desires_sparkjump = new_inputs.Item1 && new_inputs.Item2 && (!recent_inputs[0].Item1 || !recent_inputs[0].Item2) && (!recent_inputs[recent_inputs.Length-1].Item1 && !recent_inputs[recent_inputs.Length - 1].Item2);
            for (int i = 1; i < recent_inputs.Length; i++)
            {
                recent_inputs[i] = recent_inputs[i - 1];
            }
            recent_inputs[0] = new_inputs;


            sparkJumpCooldown--;

            bool flag2 = player.eatMeat >= 20 || player.maulTimer >= 15;
            if (sparkJumping) return;
            Debug.Log(sparkJumps);

            if (desires_sparkjump && player.wantToJump > 0 && player.canJump > 0 && !player.submerged && !flag2 && (player.input[0].y < 0 || player.bodyMode == BodyModeIndex.Crawl && player.input[0].x == 0 && player.input[0].y == 0) && player.Consious)
            {
                if(sparkJumps < 2)
                {
                    Debug.Log("recharging");
                    player.playerState.quarterFoodPoints -= 2 - sparkJumps;
                    sparkJumps = 2;
                    MakeSparkJumpEffect(player.firstChunk.pos);
                    player.room.PlaySound(SoundID.Fire_Spear_Pop, startpos, 0.3f + UnityEngine.Random.value * 0.3f, 0.5f + UnityEngine.Random.value * 2f);
                    sparkJumpCooldown = 5f;
                }
                else
                {
                    player.room.PlaySound(SoundID.Zapper_Zap, startpos, 0.3f + UnityEngine.Random.value * 0.3f, 0.5f + UnityEngine.Random.value * 2f);
                }
            }
            else if (desires_sparkjump && sparkJumps > 0 && !flag2 && (player.input[0].y >= 0 || (player.input[0].y < 0 &&  player.Consious && player.bodyMode != BodyModeIndex.ClimbIntoShortCut && player.onBack == null)))
            {
                sparkJumpCooldown = 5f;
                SparkJump(player.input[0]);
            }else if (desires_sparkjump)
            {
                player.room.PlaySound(SoundID.Zapper_Zap, startpos, 0.3f + UnityEngine.Random.value * 0.3f, 0.5f + UnityEngine.Random.value * 2f);
            }
        }
    }
}


