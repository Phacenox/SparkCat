using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PlayerGraphics;

namespace SparkCat
{
    public class SparkCatGraphics: PlayerGraphics
    {
        public PlayerGraphics graphics;
        public SparkCatState state;
        public LightSource myLight;
        public BodyMods bodyMods;
        public SparkCatGraphics(PlayerGraphics graphics, SparkCatState state): base(graphics)
        {
            this.graphics = graphics;
            this.state = state;
            bodyMods = new BodyMods(graphics, 12);
        }

        public void TeleportTail(Vector2 distance)
        {
            for (int i = 0; i < graphics.tail.Length; i++)
            {
                graphics.tail[i].pos += distance;
                graphics.tail[i].lastPos += distance;
            }
        }
        float LightCounter = 0;

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!owner.room.game.DEBUGMODE)
            {
                sLeaser.sprites = new FSprite[13 + bodyMods.numberOfSprites];

                sLeaser.sprites[0] = new FSprite("BodyA");
                sLeaser.sprites[0].anchorY = 0.7894737f;
                if (RenderAsPup)
                {
                    sLeaser.sprites[0].scaleY = 0.5f;
                }

                sLeaser.sprites[1] = new FSprite("HipsA");
                TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[13]
                {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
                new TriangleMesh.Triangle(4, 5, 6),
                new TriangleMesh.Triangle(5, 6, 7),
                new TriangleMesh.Triangle(8, 9, 10),
                new TriangleMesh.Triangle(9, 10, 11),
                new TriangleMesh.Triangle(12, 13, 14),
                new TriangleMesh.Triangle(2, 3, 4),
                new TriangleMesh.Triangle(3, 4, 5),
                new TriangleMesh.Triangle(6, 7, 8),
                new TriangleMesh.Triangle(7, 8, 9),
                new TriangleMesh.Triangle(10, 11, 12),
                new TriangleMesh.Triangle(11, 12, 13)
                };
                TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, customColor: false);
                sLeaser.sprites[2] = triangleMesh;
                sLeaser.sprites[3] = new FSprite("HeadA0");

                sLeaser.sprites[4] = new FSprite("LegsA0");
                sLeaser.sprites[4].anchorY = 0.25f;
                sLeaser.sprites[5] = new FSprite("PlayerArm0");
                sLeaser.sprites[5].anchorX = 0.9f;
                sLeaser.sprites[5].scaleY = -1f;
                sLeaser.sprites[6] = new FSprite("PlayerArm0");
                sLeaser.sprites[6].anchorX = 0.9f;
                sLeaser.sprites[7] = new FSprite("OnTopOfTerrainHand");
                sLeaser.sprites[8] = new FSprite("OnTopOfTerrainHand");
                sLeaser.sprites[8].scaleX = -1f;
                sLeaser.sprites[9] = new FSprite("FaceA0");
                sLeaser.sprites[11] = new FSprite("pixel");
                sLeaser.sprites[11].scale = 5f;
                sLeaser.sprites[10] = new FSprite("Futile_White");
                sLeaser.sprites[10].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                if (ModManager.MSC)
                {
                    bodyMods.InitiateSprites(sLeaser, rCam);

                    if (gown != null)
                    {
                        gownIndex = sLeaser.sprites.Length - 1;
                        gown.InitiateSprite(gownIndex, sLeaser, rCam);
                    }
                }

                AddToContainer(sLeaser, rCam, null);
            }
            else
            {
                sLeaser.sprites = new FSprite[2];
                for (int l = 0; l < 2; l++)
                {
                    FSprite fSprite = new FSprite("pixel");
                    sLeaser.sprites[l] = fSprite;
                    rCam.ReturnFContainer("Midground").AddChild(fSprite);
                    fSprite.x = -10000f;
                    fSprite.color = new Color(1f, 0.7f, 1f);
                    fSprite.scale = owner.bodyChunks[l].rad * 2f;
                    fSprite.shader = FShader.Basic;
                }
            }

            var ptr = typeof(PlayerGraphics).GetMethod("InitiateSprites").MethodHandle.GetFunctionPointer();
            var baseSprites = (Func<RoomCamera.SpriteLeaser, RoomCamera, int>)Activator.CreateInstance(typeof(Action), this, ptr);
            baseSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            sLeaser.RemoveAllSpritesFromContainer();
            if (newContainer == null)
            {
                newContainer = rCam.ReturnFContainer("Midground");
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (ModManager.MSC && i == graphics.gownIndex)
                {
                    newContainer = rCam.ReturnFContainer("Items");
                    newContainer.AddChild(sLeaser.sprites[i]);
                }
                else if (ModManager.MSC)
                {
                    if (i == 3)
                    {
                        bodyMods.AddToContainer(sLeaser, rCam, newContainer);
                    }

                    if ((i <= 6 || i >= 9) && i <= 9)
                    {
                        newContainer.AddChild(sLeaser.sprites[i]);
                    }
                    else
                    {
                        rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                    }
                }
                else if ((i > 6 && i < 9) || i > 9)
                {
                    rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                }
                else
                {
                    newContainer.AddChild(sLeaser.sprites[i]);
                }
            }
        }

        public void DrawSpritesOverride(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            bodyMods.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (state.graphic_teleporting)
                timeStacker = 1;


            var default_eye_color = JollyColor(state.player.playerState.playerNumber, 1);
            if (myLight == null && state.player.room != null)
            {
                myLight = new LightSource(state.player.firstChunk.pos, environmentalLight: true, default_eye_color, state.player);
                state.player.room.AddObject(myLight);
                myLight.colorFromEnvironment = false;
                myLight.noGameplayImpact = true;
            }
            float num = 2 + Mathf.Sin(LightCounter) * 0.2f;
            LightCounter += UnityEngine.Random.Range(0.01f, 0.1f);
            if(myLight != null && state.player.room != null)
            {
                myLight.HardSetPos(state.player.bodyChunks[0].pos);
                myLight.HardSetRad(12 + num * 7 + 12 * state.zipCharges/2);
                myLight.HardSetAlpha(Mathf.Lerp(0, (0.2f + num / 4) * state.zipCharges / 2, state.player.room.Darkness(myLight.Pos)));
            }
            else if(myLight != null)
            {
                myLight.Destroy();
                myLight = null;
            }


            orig(graphics, sLeaser, rCam, timeStacker, camPos);
            switch (state.zipCharges)
            {
                case 0:
                    sLeaser.sprites[0].color = new Color(0.01f, 0, 0, 1);
                    break;
                case 1:
                    sLeaser.sprites[0].color = Color.Lerp(default_eye_color, new Color(0.01f, 0, 0, 1), 0.5f);
                    break;
                case 2:
                    sLeaser.sprites[0].color = default_eye_color;
                    break;
            }
            sLeaser.sprites[9].color = new Color(0.01f, 0, 0, 1);
            sLeaser.sprites[10].alpha = 0f;
            sLeaser.sprites[11].alpha = 0f;
        }
        //0: upper body/back
        //1: middle body/front
        //2: tail
        //3: head
        //4: legs
        //5: right arm

        public class BodyMods
        {
            public PlayerGraphics pGraphics;
            public int numberOfSprites;
            public int startSprite;
            public int rows;
            public int lines;

            public int charge;

            public BodyMods(PlayerGraphics pGraphics, int startSprite)
            {
                this.pGraphics = pGraphics;
                this.startSprite = startSprite;

                rows = 8;
                lines = 2;
                numberOfSprites = rows * lines;
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
            {
                if (sLeaser.sprites.Length <= 13)
                    return;
                for (int i = startSprite; i < startSprite + numberOfSprites; i++)
                {
                    newContainer.AddChild(sLeaser.sprites[i]);
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                for (int i = 0; i < rows; i++)
                {
                    float f = Mathf.InverseLerp(0f, rows - 1, i);
                    float s = Mathf.Lerp(0.2f, 0.95f, Mathf.Pow(f, 0.8f));
                    PlayerSpineData playerSpineData = pGraphics.SpinePosition(s, timeStacker);

                    Color color = SlugcatColor(pGraphics.CharacterForColor);

                    float num = 0.8f * Mathf.Pow(f, 0.5f);
                    Color color2 = Color.Lerp(color, Color.Lerp(new Color(1f, 1f, 1f), color, 0.3f), 0.2f + num);
                    for (int j = 0; j < lines; j++)
                    {
                        float num3 = ((float)j + ((i % 2 != 0) ? 0f : 0.5f)) / (float)(lines - 1);
                        num3 = -1f + 2f * num3;
                        if (num3 < -1f)
                        {
                            num3 += 2f;
                        }
                        else if (num3 > 1f)
                        {
                            num3 -= 2f;
                        }

                        num3 = Mathf.Sign(num3) * Mathf.Pow(Mathf.Abs(num3), 0.6f);
                        Vector2 vector = playerSpineData.pos + playerSpineData.perp * (playerSpineData.rad + 0.5f) * num3;
                        sLeaser.sprites[startSprite + i * lines + j].x = vector.x - camPos.x;
                        sLeaser.sprites[startSprite + i * lines + j].y = vector.y - camPos.y;
                        sLeaser.sprites[startSprite + i * lines + j].color = new Color(1f, 0f, 0f);
                        sLeaser.sprites[startSprite + i * lines + j].rotation = Custom.VecToDeg(playerSpineData.dir);
                        sLeaser.sprites[startSprite + i * lines + j].scaleX = Custom.LerpMap(Mathf.Abs(num3), 0.4f, 1f, 1f, 0f);
                        sLeaser.sprites[startSprite + i * lines + j].scaleY = 1f;

                        if (ModManager.CoopAvailable && pGraphics.player.IsJollyPlayer)
                        {
                            sLeaser.sprites[startSprite + i * lines + j].color = JollyColor(pGraphics.player.playerState.playerNumber, 1);
                        }
                        else if (CustomColorsEnabled())
                        {
                            sLeaser.sprites[startSprite + i * lines + j].color = CustomColorSafety(1);
                        }
                        else if (pGraphics.CharacterForColor == SlugcatStats.Name.White || pGraphics.CharacterForColor == SlugcatStats.Name.Yellow)
                        {
                            sLeaser.sprites[startSprite + i * lines + j].color = Color.gray;
                        }
                        else
                        {
                            sLeaser.sprites[startSprite + i * lines + j].color = color2;
                        }

                        if(charge == 1)
                        {
                            sLeaser.sprites[startSprite + i * lines + j].color = Color.Lerp(sLeaser.sprites[startSprite + i * lines + j].color, new Color(0.01f, 0.01f, 0.01f, 1), 0.5f);
                        }else if(charge == 0)
                        {
                            sLeaser.sprites[startSprite + i * lines + j].color = new Color(0.01f, 0.01f, 0.01f, 1);
                        }
                    }
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                var old = sLeaser.sprites;
                sLeaser.sprites = new FSprite[sLeaser.sprites.Length + numberOfSprites];
                old.CopyTo(sLeaser.sprites, 0);
                Debug.Log(sLeaser.sprites);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < lines; j++)
                    {
                        sLeaser.sprites[startSprite + i * lines + j] = new FSprite("tinyStar");
                    }
                }
            }

        }
    }
}
