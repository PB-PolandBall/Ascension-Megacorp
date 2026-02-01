using Fortified;
using RimWorld;
using UnityEngine;
using Verse;

namespace USAC
{
    // 定义机兵平心降落器逻辑
    // 实现重绘逻辑以显示内部机兵
    public class Skyfaller_MechIncoming : Skyfaller
    {
        // 获取获取容器内部机兵对象
        private Pawn InnerMech
        {
            get
            {
                if (!innerContainer.Any) return null;
                if (innerContainer[0] is Building_MechCapsule capsule && capsule.HasMech)
                {
                    return capsule.Mech;
                }
                return null;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Pawn mech = InnerMech;
            if (mech != null)
            {
                // 执行容器内部机兵绘制
                mech.Drawer.renderer.DynamicDrawPhaseAt(DrawPhase.Draw, drawLoc, Rotation, false);
            }
            else
            {
                // 执行降落器默认图形绘制
                base.DrawAt(drawLoc, flip);
            }

            // 执行地面落点阴影绘制
            DrawDropSpotShadow();
        }

        protected override void SpawnThings()
        {
            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                Thing thing = innerContainer[i];

                // 从容器移除器中移除
                innerContainer.Remove(thing);

                // 直接生成物体直接生成
                GenSpawn.Spawn(thing, base.Position, base.Map, thing.Rotation);
            }
        }
    }
}
