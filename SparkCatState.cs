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
        public ChargeablesState chargeablesState;
        public SparkCatState(Player player)
        {
            this.player = player;
            zipChargesStored = maxZipChargesStored;
            chargeablesState = new ChargeablesState(this);
        }
        const int input_frame_window = 5;
        public float zipLength;

        public const int maxZipChargesStored = 12;
        public int zipChargesStored = 12;
        public int zipChargesReady = 2;

        int recharge_timer = 40;
        bool grounded_since_last_zip = false;

        int iterator_recharge = 30;

        public bool zipping
        {
            get => zipFrame > 0;
            set => zipFrame = value ? 6 : 0;
        }
        public Vector2 zipStartPos;
        public Vector2 zipEndPos;
        public IntVector2 zipInputDirection;
        public int zipFrame = 0;

        public bool graphic_teleporting = false;
        public int zipCooldown = 0;

        public int rechargeZipStorage(int max_available)
        {
            if (max_available == 0 || maxZipChargesStored == zipChargesStored) return 0;

            var ret = maxZipChargesStored - zipChargesStored;
            ret = Mathf.Min(ret, max_available);
            zipChargesStored += ret;

            MakeZipEffect(player.firstChunk.pos, 3, 0.6f, player);
            player.room.PlaySound(Sounds.Recharge, player.mainBodyChunk.pos, 0.3f + UnityEngine.Random.value * 0.1f, 0.8f + UnityEngine.Random.value * 0.5f);
            player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));
            return ret;
        }


        public void Zip(InputPackage direction)
        {
            grounded_since_last_zip = false;
            zipInputDirection = direction.IntVec;
            if (player.wantToJump > 0) player.wantToJump = 0;
            zipChargesReady--;
            zipStartPos = player.firstChunk.pos;
            zipEndPos = zipStartPos + zipInputDirection.ToVector2().normalized * zipLength;

            IntVector2 tilestart = player.room.GetTilePosition(player.firstChunk.pos);
            IntVector2 tileend = player.room.GetTilePosition(zipEndPos);
            List<IntVector2> tiles = new List<IntVector2>();
            player.room.RayTraceTilesList(tilestart.x, tilestart.y, tileend.x, tileend.y, ref tiles);
            for (int i = 1; i < tiles.Count; i++)
            {
                if (player.room.GetTile(tiles[i]).Solid)
                {
                    zipEndPos = player.room.MiddleOfTile(tiles[i - 1]);
                    break;
                }
            }

            zipping = true;
            MakeZipEffect(zipStartPos, 6, 1f, player);
            MakeZipEffect(zipEndPos, 3, 0.6f);
            player.room.PlaySound(Sounds.QuickZap, zipEndPos, 0.3f + UnityEngine.Random.value * 0.1f, 0.8f + UnityEngine.Random.value * 1.7f);
            player.room.InGameNoise(new InGameNoise(zipEndPos, 800f, player, 1f));
        }


        public void DoZip()
        {
            graphic_teleporting = false;
            zipFrame--;
            if(zipFrame == 1)
                player.room.AddObject(new ZipSwishEffect(player.firstChunk.pos, zipEndPos, 5.5f, 0.4f, Color.white));

            if(zipFrame == 0)
            {
                graphic_teleporting = true;
                zipStartPos = player.firstChunk.pos;
                if (zipInputDirection == new IntVector2(0,0))
                    zipEndPos = zipStartPos + Vector2.up * 3;
                MakeZipEffect(zipStartPos, 3, 0.6f);
                MakeZipEffect(zipEndPos, 6, 1f, player);
                var distance = zipEndPos -  zipStartPos;

                if (player.slugOnBack != null && player.slugOnBack.HasASlug)
                    ObjectTeleports.TrySmoothTeleportObject(player.slugOnBack.slugcat, distance);
                if(player.spearOnBack != null)
                    ObjectTeleports.TrySmoothTeleportObject(player.spearOnBack.spear, distance);
                ObjectTeleports.TrySmoothTeleportObject(player, distance);
                foreach (var i in player.grasps)
                    if(i != null)
                        ObjectTeleports.TrySmoothTeleportObject(i.grabbed, distance);

                var target_vel = (zipEndPos - zipStartPos).normalized * 3;
                if (Mathf.Abs(target_vel.y) < 0.7f)
                    target_vel.y = 0.1f * Mathf.Sign(player.bodyChunks[0].vel.y);
                if (player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.1f)
                    target_vel = zipInputDirection.ToVector2().normalized * 4;

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

        public void ClassMechanicsSparkCat(float zipLength)
        {
            this.zipLength = zipLength;
            #region recharging
            if (player.canJump > 0)
                grounded_since_last_zip = true;
            if (player.bodyMode == BodyModeIndex.ZeroG || player.gravity <= 0.2f && zipFrame < -5)
            {
                //assume encapsulating check means inside iterator. TODO?: make more specific
                iterator_recharge--;
                recharge_timer--;
            }
            else if (grounded_since_last_zip)
            {
                recharge_timer--;
            }
            if (iterator_recharge <= 0 && zipChargesStored < maxZipChargesStored)
            {
                zipChargesStored++;
                iterator_recharge = 30;
            }
            if (zipChargesStored > 0 && recharge_timer <= 0 && zipChargesReady < 2)
            {
                recharge_timer = 40;
                zipChargesStored--;
                zipChargesReady++;
            } else if (zipChargesReady == 2)
            {
                recharge_timer = 40;
            }
            #endregion

            //determine inputs with buffer
            bool desires_sparkjump = player.input[0].jmp && player.input[0].pckp;
            if (desires_sparkjump)
            {
                for (int i = 1; i < Math.Min(player.input.Length, input_frame_window); i++)
                {
                    if (!player.input[i].jmp && !player.input[i].pckp)
                        break;
                    if (player.input[i].jmp && player.input[i].pckp)
                    {
                        desires_sparkjump = false;
                        break;
                    }
                    if (i == Math.Min(player.input.Length - 1, input_frame_window - 1))
                    {
                        if (player.input[i].jmp || player.input[i].pckp)
                            desires_sparkjump = false;
                    }
                }
            }

            if (zipCooldown > 0)
                zipCooldown--;

            if (zipping || player.eatMeat >= 20 || player.maulTimer >= 15 || !player.Consious || zipCooldown > 0) return;

            //recharge
            if (desires_sparkjump && !player.submerged
                && (player.canJump > 0 || player.bodyMode == BodyModeIndex.CorridorClimb)
                && (player.bodyMode != BodyModeIndex.CorridorClimb && player.bodyMode != BodyModeIndex.ClimbingOnBeam && player.input[0].y < 0
                    || (player.bodyMode == BodyModeIndex.Crawl || player.bodyMode == BodyModeIndex.CorridorClimb || player.bodyMode == BodyModeIndex.ClimbingOnBeam) && player.input[0].x == 0 && player.input[0].y == 0))
            {
                zipCooldown = 5;
                if (player.playerState.foodInStomach > 0 && rechargeZipStorage(6) > 0)
                    player.SubtractFood(1);
                else
                    DoFailureEffect();
            }//zip
            else if (desires_sparkjump)
            {
                zipCooldown = 5;
                if (zipChargesReady > 0)
                    Zip(player.input[0]);
                else
                    DoFailureEffect();
            }
        }
        public void DoFailureEffect()
        {

            player.room.PlaySound(Sounds.NoDischarge, player.mainBodyChunk.pos, 0.2f + UnityEngine.Random.value * 0.1f, 0.7f + UnityEngine.Random.value * 0.4f);
            player.room.InGameNoise(new InGameNoise(player.mainBodyChunk.pos, 200f, player, 1f));
            Vector2 vector = Custom.RNV();
            player.room.AddObject(new Spark(player.firstChunk.pos + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 6));
        }
        public void MakeZipEffect(Vector2 where, float size, float alpha, Player follow = null)
        {
            player.room.AddObject(new ZipFlashEffect(where, size, alpha, 3, Color.white, follow));
            for (int j = 0; j < 10; j++)
            {
                Vector2 vector = Custom.RNV();
                player.room.AddObject(new Spark(where + vector * UnityEngine.Random.value * 4f, vector * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white * 0.8f, null, 4, 8));
            }
        }
    }
}


