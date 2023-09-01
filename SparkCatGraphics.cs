using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SparkCat
{
    public class SparkCatGraphics
    {
        public PlayerGraphics graphics;
        public SparkCatState state;
        public LightSource myLight;
        public SparkCatGraphics(PlayerGraphics graphics, SparkCatState state)
        {
            this.graphics = graphics;
            this.state = state;
        }

        public void TeleportTail(Vector2 distance)
        {
            for(int i = 0; i < graphics.tail.Length; i++)
            {
                graphics.tail[i].pos += distance;
                graphics.tail[i].lastPos += distance;
            }
        }
        float LightCounter = 0;
        public void DrawSpritesOverride(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (state.graphic_teleporting)
                timeStacker = 1;


            var default_eye_color = PlayerGraphics.JollyColor(state.player.playerState.playerNumber, 1);
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
        }
        //0: upper body/back
        //1: middle body/front
        //2: tail
        //3: head
        //4: legs
        //5: right arm
    }
}
