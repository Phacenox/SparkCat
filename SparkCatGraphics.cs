using MoreSlugcats;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase.Features;
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
        const int original_sprite_count = 13;
        public SparkCatState state;
        public LightSource myLight;
        public BodyMods bodyMods;
        Color baseElectricColor;
        ElectricityColor eyeColor;
        ElectricityColor backColor;


        public SparkCatGraphics(Player p, SparkCatState state) : base(p)
        {
            this.state = state;
            if (CustomColorsEnabled())
            {
                baseElectricColor = CustomColorSafety(1);
            }
            else
            {
                baseElectricColor = PlayerColor.GetCustomColor(this, 1);
            }

            bodyMods = new BodyMods(this, original_sprite_count - 1);
            tail[1].rad -= 1;
            tail[2].rad += 1;
            eyeColor = new ElectricityColor(baseElectricColor, 0.3f);
            backColor = new ElectricityColor(baseElectricColor);
        }

        float LightCounter = 0;

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!owner.room.game.DEBUGMODE)
            {
                sLeaser.sprites = new FSprite[original_sprite_count + bodyMods.numberOfSprites];

                sLeaser.sprites[0] = new FSprite("BodyA");
                sLeaser.sprites[0].anchorY = 0.7894737f;
                if (RenderAsPup)
                    sLeaser.sprites[0].scaleY = 0.5f;
                sLeaser.sprites[1] = new FSprite("HipsA");
                TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[13]
                {
                new TriangleMesh.Triangle(0, 1, 2), new TriangleMesh.Triangle(1, 2, 3), new TriangleMesh.Triangle(4, 5, 6),
                new TriangleMesh.Triangle(5, 6, 7), new TriangleMesh.Triangle(8, 9, 10), new TriangleMesh.Triangle(9, 10, 11),
                new TriangleMesh.Triangle(12, 13, 14), new TriangleMesh.Triangle(2, 3, 4), new TriangleMesh.Triangle(3, 4, 5),
                new TriangleMesh.Triangle(6, 7, 8), new TriangleMesh.Triangle(7, 8, 9), new TriangleMesh.Triangle(10, 11, 12),
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
                bodyMods.InitiateSprites(sLeaser);
                if (ModManager.MSC && gown != null)
                {
                    gownIndex = sLeaser.sprites.Length - 1;
                    gown.InitiateSprite(gownIndex, sLeaser, rCam);
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
            //up two levels (base.base)
            if (DEBUGLABELS != null && DEBUGLABELS.Length != 0)
            {
                DebugLabel[] dEBUGLABELS = DEBUGLABELS;
                foreach (DebugLabel debugLabel in dEBUGLABELS)
                {
                    rCam.ReturnFContainer("HUD").AddChild(debugLabel.label);
                }
            }
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            sLeaser.RemoveAllSpritesFromContainer();
            if (newContainer == null)
                newContainer = rCam.ReturnFContainer("Midground");

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (ModManager.MSC && i == gownIndex)
                {
                    newContainer = rCam.ReturnFContainer("Items");
                    newContainer.AddChild(sLeaser.sprites[i]);
                }
                else if (ModManager.MSC)
                {
                    if( i < original_sprite_count)
                    {
                        if (i == 3)
                            bodyMods.AddToContainer(sLeaser, newContainer);

                        if ((i <= 6 || i >= 9) && i <= 9)
                            newContainer.AddChild(sLeaser.sprites[i]);
                        else
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

        int curl_side = -1;
        public override void Update()
        {
            float stiffness = 0.35f * state.zipChargesReady / 2;
            float[] desired_tail_angles_deg = new float[4]
            {
                25f, 20f, 15f, 10f
            };
            if (player.bodyMode == Player.BodyModeIndex.ZeroG || player.gravity <= 0.1f)
                desired_tail_angles_deg = new float[] { 0, 0, 0, 0 };

            base.Update();
            bodyMods.Update();
            eyeColor.Update();
            backColor.Update();
            if (!player.dead && !player.Sleeping)
            {
                if(player.animation != Player.AnimationIndex.Roll && player.animation != Player.AnimationIndex.Flip)
                {
                    var new_curl_side = player.bodyChunks[1].pos.x < player.bodyChunks[0].pos.x ? 1 : -1;
                    if (curl_side != new_curl_side && Mathf.Abs(player.bodyChunks[1].pos.x - player.bodyChunks[0].pos.x) > 0.5f)
                        curl_side = new_curl_side;
                }


                var desired_angle = (player.bodyChunks[1].pos - player.bodyChunks[0].pos).normalized;
                desired_angle = Custom.rotateVectorDeg(desired_angle, curl_side * desired_tail_angles_deg[0]);

                for(int i = 1; i < tail.Length; i++)
                {
                    var desired_tail_position = (tail[i].pos - tail[i-1].pos).magnitude * desired_angle + tail[i-1].pos;

                    tail[i].vel += (desired_tail_position - tail[i].pos).normalized * Mathf.Pow(Mathf.Max((desired_tail_position - tail[i].pos).magnitude, 0), 0.3f) * stiffness;
                    tail[i].vel.y += stiffness * 0.25f * ((4 - i) / 3);

                    desired_angle = (tail[i].pos - tail[i - 1].pos).normalized;
                    desired_angle = Custom.rotateVectorDeg(desired_angle, curl_side * desired_tail_angles_deg[i]);
                }
            }
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            if (state.graphic_teleporting)
                timeStacker = 1;

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[1].scaleX *= 0.9f;

            bodyMods.DrawSprites(sLeaser, timeStacker, camPos, state.zipChargesReady);

            DrawLight(baseElectricColor);

            sLeaser.sprites[0].color = Color.Lerp(new Color(0.01f, 0.01f, 0.01f), backColor.Color, (float)state.zipChargesStored / SparkCatState.maxZipChargesStored);
            sLeaser.sprites[9].color = Color.Lerp(new Color(0.01f, 0.01f, 0.01f), eyeColor.Color, (float)state.zipChargesStored / SparkCatState.maxZipChargesStored * 0.7f + 0.3f);
        }

        public void DrawLight(Color default_eye_color)
        {
            if (myLight == null && state.player.room != null)
            {
                myLight = new LightSource(state.player.firstChunk.pos, environmentalLight: true, default_eye_color, state.player);
                state.player.room.AddObject(myLight);
                myLight.colorFromEnvironment = false;
                myLight.noGameplayImpact = true;
            }
            float num = 2 + Mathf.Sin(LightCounter) * 0.2f;
            LightCounter += UnityEngine.Random.Range(0.01f, 0.1f);
            if (myLight != null && state.player.room != null)
            {
                float charge_scaling = ((float)state.zipChargesReady / 2 + (float)state.zipChargesStored / SparkCatState.maxZipChargesStored) / 2;

                myLight.HardSetPos(state.player.bodyChunks[0].pos);
                myLight.HardSetRad(12 + num * 7 + 22 * charge_scaling);
                myLight.HardSetAlpha(Mathf.Lerp(0, (0.1f + num / 4) * charge_scaling, state.player.room.Darkness(myLight.Pos)));
            }
            else if (myLight != null)
            {
                myLight.Destroy();
                myLight = null;
            }
        }

        public class BodyMods
        {
            public SparkCatGraphics pGraphics;
            public int numberOfSprites;
            public int startSprite;
            public int rows;
            public int lines;
            public ElectricityColor[] fluxes;

            public BodyMods(SparkCatGraphics pGraphics, int startSprite)
            {
                this.pGraphics = pGraphics;
                this.startSprite = startSprite;
                rows = 7;
                lines = 2;
                numberOfSprites = rows * lines;
                fluxes = new ElectricityColor[numberOfSprites];
                for (int i = 0; i < fluxes.Length; i++)
                    fluxes[i] = new ElectricityColor(pGraphics.baseElectricColor);
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, FContainer newContainer)
            {
                for (int i = startSprite; i < startSprite + numberOfSprites; i++)
                    newContainer.AddChild(sLeaser.sprites[i]);
            }

            readonly AnimationCurve TailColorCurve = new AnimationCurve()
            {
                keys = new Keyframe[]
                {
                    new Keyframe(0, 0.3f, 0, 1), new Keyframe(0.8f, 1, 0, 0), new Keyframe(1, 0.65f, 1, 0)
                }
            };

            public void Update()
            {
                if (charge_anim > 0)
                    charge_anim--;
                foreach (var i in fluxes)
                    i.Update();
            }

            int last_charges = 0;
            int recent_charges = 0;
            int charge_anim = 0;
            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos, int charges)
            {
                if(charges != recent_charges)
                {
                    last_charges = recent_charges;
                    recent_charges = charges;
                    charge_anim = 5;
                }
                float curve_scalar = 1 / (Mathf.Lerp(last_charges, recent_charges, Mathf.Max(5 - (charge_anim - timeStacker), 0) / 5) / 2);
                for (int row = 0; row < rows; row++)
                {
                    float row_value = Mathf.InverseLerp(0f, rows - 1, row);
                    float spine_value = Mathf.Lerp(0.0f, 1f, Mathf.Pow(row_value, 0.8f));
                    PlayerSpineData playerSpineData = pGraphics.SpinePosition(spine_value, timeStacker);

                    for (int line = 0; line < lines; line++)
                    {
                        int index = startSprite + row * lines + line;

                        float perp_on_tail_amount = 1 - 2 * line;
                        Vector2 vector = playerSpineData.pos + playerSpineData.perp * (playerSpineData.rad + 0.5f) * perp_on_tail_amount;
                        sLeaser.sprites[index].x = vector.x - camPos.x;
                        sLeaser.sprites[index].y = vector.y - camPos.y;
                        sLeaser.sprites[index].rotation = Custom.VecToDeg(playerSpineData.dir);
                        sLeaser.sprites[index].scaleX = 0.8f;
                        sLeaser.sprites[index].scaleY = 1.5f;

                        var rBaseColor = fluxes[row].Color;

                        if (row_value * curve_scalar > 1.1f)
                            rBaseColor = new Color(0.01f, 0.01f, 0.01f, 0.5f);
                        else
                            rBaseColor = Color.Lerp(new Color(0.01f, 0.01f, 0.01f, 0.5f), rBaseColor, TailColorCurve.Evaluate(row_value * curve_scalar));
                        sLeaser.sprites[index].color = rBaseColor;
                    }
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser)
            {
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < lines; j++)
                        sLeaser.sprites[startSprite + i * lines + j] = new FSprite("tinyStar");
            }
        }
        public class ElectricityColor
        {
            public float speedscalar = 1.0f;
            float fluxSpeed;
            float fluxTimer;
            Color _color;
            public Color Color
            {
                get
                {
                    return Color.Lerp(_color, Color.white, Mathf.Abs(Mathf.Sin(fluxTimer)));
                }
                set { _color = value; }
            }

            public ElectricityColor(Color color, float speedscalar = 1.0f)
            {
                _color = color;
                this.speedscalar = speedscalar;
                ResetFluxSpeed();
            }
            const float resetovercap = 6.2831855f;
            public void ResetFluxSpeed()
            {
                fluxSpeed = UnityEngine.Random.value * 0.2f + 0.025f;
                while (fluxTimer > resetovercap)
                {
                    fluxTimer -= resetovercap;
                }
            }
            public void Update()
            {
                fluxTimer += fluxSpeed * speedscalar;
                if (fluxTimer > resetovercap)
                    ResetFluxSpeed();
            }

        }
    }
}
