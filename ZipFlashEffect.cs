using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SparkCat
{
    internal class ZipFlashEffect: CosmeticSprite
    {
        public float rad;

        public float life;

        public float lastLife;

        public int lifeTime;

        public float alpha;

        public Player follow;

        public Color lightColor;
        public ZipFlashEffect(Vector2 pos, float rad, float alpha, int lifeTime, Color lightColor, Player follow = null)
        {
            base.pos = pos;
            lastPos = pos;
            this.rad = rad;
            this.alpha = alpha;
            this.lifeTime = lifeTime;
            this.lightColor = lightColor;
            life = 1f;
            lastLife = 0f;
            this.follow = follow;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (follow != null)
                pos = follow.firstChunk.pos;
            lastLife = life;
            life -= 1f / (float)lifeTime;
            if (!(lastLife < 0f))
            {
                return;
            }
            Destroy();
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite("Futile_White");
            sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["FlatLight"];
            sLeaser.sprites[0].color = lightColor;
            for (int i = 1; i < 3; i++)
            {
                sLeaser.sprites[i] = new FSprite("Futile_White");
                sLeaser.sprites[i].shader = rCam.room.game.rainWorld.Shaders["LightSource"];
                sLeaser.sprites[i].color = lightColor;
            }

            AddToContainer(sLeaser, rCam, null);
        }

        AnimationCurve curve = new AnimationCurve
        {
            keys = new Keyframe[] { new Keyframe(0, 0, 0, 10), new Keyframe(0.5f, 1), new Keyframe(1, 0, 10, 0) }
        };
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < 3; i++)
            {
                sLeaser.sprites[i].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[i].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
            }
            float scale = curve.Evaluate((lifeTime - life - timeStacker) / lifeTime);
            Debug.Log(scale);
            sLeaser.sprites[0].alpha = scale * alpha * 0.8f;
            sLeaser.sprites[0].scale = scale * rad;
            /*
            for (int j = 1; j < 3; j++)
            {
                sLeaser.sprites[j].alpha = Mathf.Pow(num, 0.5f) * alpha;
                sLeaser.sprites[j].scale = Mathf.Pow(num, 0.5f) * rad / 8f;
            }

            sLeaser.sprites[1].color = lightColor;
            sLeaser.sprites[2].color = Color.Lerp(lightColor, new Color(1f, 1f, 1f), UnityEngine.Random.value * Mathf.Pow(num, 0.5f));*/
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Bloom");
            }

            base.AddToContainer(sLeaser, rCam, newContatiner);
        }
    }
}
