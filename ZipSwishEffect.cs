using UnityEngine;

namespace SparkCat
{
    internal class ZipSwishEffect: CosmeticSprite
    {
        public float rad;

        public float life;
        public float lifeTime;
        public float lastLife;
        public float alpha;
        public Vector2 start_pos;
        public Vector2 end_pos;
        public Color lightColor;

        public ZipSwishEffect(Vector2 start_pos, Vector2 end_pos, float rad, float alpha, Color lightColor)
        {
            this.start_pos = start_pos;
            this.end_pos = end_pos;
            this.rad = rad;
            this.alpha = alpha;
            this.lifeTime = 4;
            this.life = 1;
            this.lastLife = 0;
            pos = start_pos;
            lastPos = pos;
            this.lightColor = lightColor;
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            pos = life * (end_pos - start_pos) + start_pos;
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
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White");
            sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["FlatLight"];
            sLeaser.sprites[0].color = lightColor;

            AddToContainer(sLeaser, rCam, null);
        }

        AnimationCurve curve = new AnimationCurve
        {
            keys = new Keyframe[] { new Keyframe(0, 0, 0, 1), new Keyframe(0.5f, 1), new Keyframe(1, 0, 1, 0) }
        };
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            float state_t = 1 - life + (timeStacker / lifeTime);

            sLeaser.sprites[0].x = Mathf.Lerp(start_pos.x, end_pos.x, state_t) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(start_pos.y, end_pos.y, state_t) - camPos.y;

            float alpha = curve.Evaluate(state_t);
            sLeaser.sprites[0].alpha = this.alpha * alpha * 0.8f;
            sLeaser.sprites[0].scale = rad;

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
