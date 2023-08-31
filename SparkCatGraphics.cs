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
        PlayerGraphics graphics;
        public SparkCatGraphics(PlayerGraphics graphics)
        {
            this.graphics = graphics;
        }

        public void TeleportTail(Vector2 distance)
        {
            for(int i = 0; i < graphics.tail.Length; i++)
            {
                graphics.tail[i].pos += distance;
                graphics.tail[i].lastPos += distance;
            }
        }
    }
}
