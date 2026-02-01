using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USAC
{
    // 定义物资箱垂直降落逻辑类
    public class Skyfaller_CrateIncoming : Skyfaller
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 绘制容器内首个物资箱物品
            if (innerContainer.Any)
            {
                innerContainer[0].Graphic.Draw(drawLoc, Rot4.North, innerContainer[0]);
            }
            else
            {
                base.DrawAt(drawLoc, flip);
            }
            DrawDropSpotShadow();
        }

        protected override void Impact()
        {
            // 执行落点位置物体生成逻辑
            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                Thing thing = innerContainer[i];
                innerContainer.Remove(thing);
                GenSpawn.Spawn(thing, Position, Map, thing.Rotation);
            }

            // 执行落地位置动态效果生成
            FleckMaker.ThrowDustPuff(Position.ToVector3Shifted(), Map, 2f);
            if (def.skyfaller.impactSound != null)
            {
                def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map)));
            }

            Destroy();
        }
    }
}
