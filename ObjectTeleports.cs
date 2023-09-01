using IL.MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SparkCat
{
    public static class ObjectTeleports
    {
        public static void TrySmoothTeleportObject(PhysicalObject item, Vector2 amount)
        {
            foreach(var i in item.bodyChunks)
            {
                i.pos += amount;
                i.lastPos += amount;
                i.lastLastPos += amount;
            }
            if(item is Player p)
            {
                var g = p.graphicsModule as PlayerGraphics;
                foreach (var i in g.hands)
                {
                    i.pos += amount;
                    i.lastPos += amount;
                }
                foreach (var i in g.tail)
                {
                    i.pos += amount;
                    i.lastPos += amount;
                }
            }
            else if (item is FlyLure f)
            {
                foreach (var i in f.stalk)
                {
                    i.pos += amount;
                    i.lastPos += amount;
                }
                foreach (var i in f.lumps)
                {
                    i.pos += amount;
                    i.lastPos += amount;
                }
            }
            else if (item is SlimeMold s)
            {
                for (int i = 0; i < s.slime.GetLength(0); i++)
                {
                    s.slime[i, 0] += amount;
                    s.slime[i, 1] += amount;
                }
            }
            /*
            if(item is JellyFish j)
            {

            }
            if(item is KarmaFlower k)
            {
                
            }
            if(item is Mushroom m)
            {

            }
            if (item is FirecrackerPlant f)
            {

            }*/
        }
    }
}
