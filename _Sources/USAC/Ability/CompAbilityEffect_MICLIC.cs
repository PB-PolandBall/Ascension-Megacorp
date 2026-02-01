using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;


namespace USAC
{
    // 定义火箭排雷索技能组件类
    public class CompAbilityEffect_MICLIC : CompAbilityEffect
    {
        private new CompProperties_AbilityMICLIC Props => (CompProperties_AbilityMICLIC)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned) return;

            // 执行排雷火箭实例生成与发射
            Projectile projectile = (Projectile)GenSpawn.Spawn(Props.projectileDef, pawn.Position, pawn.Map);
            projectile.Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.All);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (target.IsValid && parent.pawn.Map != null)
            {
                Vector3 start = parent.pawn.DrawPos;
                Vector3 end = target.CenterVector3;
                float totalDist = (end - start).MagnitudeHorizontal();
                float shotAngle = start.AngleToFlat(end);

                // 执行排雷主索预览轨迹线渲染
                GenDraw.DrawLineBetween(start, end, SimpleColor.White);


                int maxSegments = 6;
                float interval = totalDist / (maxSegments + 1);

                // 执行模拟段落部署位置视觉渲染
                for (int i = 1; i <= maxSegments; i++)
                {
                    Vector3 pos = start + (end - start).normalized * (interval * i);
                    IntVec3 cell = pos.ToIntVec3();
                    if (cell.InBounds(parent.pawn.Map))
                    {
                        DrawExplosionPreview(cell, shotAngle, parent.pawn.Map);
                    }
                }

                // 绘制火箭落点爆炸预览
                DrawExplosionPreview(target.Cell, shotAngle, parent.pawn.Map);
            }
        }

        private void DrawExplosionPreview(IntVec3 center, float angle, Map map)
        {
            var damageWorker = DamageDefOf.Bomb.Worker;
            var allCells = damageWorker.ExplosionCellsToHit(center, map, 5.9f, null, null, null);
            List<IntVec3> filtered = new List<IntVec3>();
            foreach (var cell in allCells)
            {
                float cellAngle = center.ToVector3Shifted().AngleToFlat(cell.ToVector3Shifted());
                if (Mathf.Abs(Mathf.DeltaAngle(angle, cellAngle)) <= 90.5f)
                {
                    filtered.Add(cell);
                }
            }
            GenDraw.DrawFieldEdges(filtered, Color.white);
        }
    }

    public class CompProperties_AbilityMICLIC : CompProperties_AbilityEffect
    {
        public ThingDef projectileDef;

        public CompProperties_AbilityMICLIC()
        {
            compClass = typeof(CompAbilityEffect_MICLIC);
        }
    }
}
