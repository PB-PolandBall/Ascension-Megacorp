using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace USAC
{
    // 定义全程牵引物逻辑
    [StaticConstructorOnStartup]
    public class Projectile_MICLIC_Towed : Projectile_Explosive
    {
        private class TowedNode
        {
            public float fraction; // 记录节点位置比例
            public float sagFactor; // 记录下垂系数
            public float extraInertia; // 记录水平惯性偏移
            public Vector3 lastGroundPos; // 记录爆炸地面坐标
        }

        private List<TowedNode> nodes = new List<TowedNode>();
        private float? cachedArcHeightFactor = null;
        private static readonly Material CableMat = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, new Color(0.2f, 0.2f, 0.2f));

        private void EnsureNodesInitialized()
        {
            if (nodes.Count == 0)
            {
                for (int i = 1; i <= 6; i++)
                    nodes.Add(new TowedNode { fraction = (float)i / 7f, sagFactor = 0f });
            }
        }

        private bool isLanded = false;
        private int landingTick = -1;
        private Vector3 impactPos;

        protected override void Tick()
        {
            base.Tick();
            if (!this.Spawned) return;

            // 保持发射者处于战斗等待
            if (!isLanded && launcher is Pawn pawn && pawn.Spawned)
            {
                if (pawn.CurJob != null && pawn.CurJob.def != JobDefOf.Wait_Combat)
                {
                    pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_Combat, 10), Verse.AI.JobCondition.InterruptForced);
                }
            }

            // 处理落地物理表现
            if (isLanded)
            {
                foreach (var node in nodes)
                {
                    // 计算空中节点前进惯性
                    if (node.sagFactor < 1f)
                    {
                        node.extraInertia += 0.003f; // 增加节点空中惯性
                    }
                    node.sagFactor = Mathf.Min(1f, node.sagFactor + 0.05f);
                }

                // 执行全线延迟起爆
                if (Find.TickManager.TicksGame >= landingTick + 30)
                {
                    SyncExplodeAll();
                }
                return;
            }

            EnsureNodesInitialized();

            // 处理飞行过程下坠
            foreach (var node in nodes)
            {
                node.sagFactor = Mathf.Min(1f, node.sagFactor + 0.008f);
            }
        }

        private void SyncExplodeAll()
        {
            float angle = origin.AngleToFlat(destination);
            // 执行全线同步爆炸
            foreach (var node in nodes)
            {
                ExplodeAt(node.lastGroundPos, angle);
            }
            ExplodeAt(impactPos, angle);
            this.Destroy(); // 销毁弹体
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 获取当前绘制坐标
            Vector3 currentPos = isLanded ? impactPos : drawLoc;

            float totalDist = (destination - origin).MagnitudeHorizontal();
            float coveredDist = (isLanded ? (impactPos - origin).MagnitudeHorizontal() : (ExactPosition - origin).MagnitudeHorizontal());
            float progress = totalDist > 0.01f ? Mathf.Clamp01(coveredDist / totalDist) : 0f;

            float arcHeight = 0f;
            if (!isLanded)
            {
                if (cachedArcHeightFactor == null) cachedArcHeightFactor = def.projectile.arcHeightFactor;
                arcHeight = cachedArcHeightFactor.Value * GenMath.InverseParabola(progress);
            }

            Vector3 visualRocketPos = currentPos;
            visualRocketPos.z += arcHeight;

            float angle = origin.AngleToFlat(destination);
            Graphic.Draw(visualRocketPos, this.Rotation, this, angle);

            DrawChain(visualRocketPos, progress, angle);
        }

        private void DrawChain(Vector3 currentRocketPos, float progress, float shotAngle)
        {
            EnsureNodesInitialized();
            // 获取发射者实时坐标
            Vector3 startPos = (launcher != null && launcher.Spawned) ? launcher.DrawPos : origin;
            Vector3 prev = startPos;

            Graphic segmentGraphic = USAC_DefOf.USAC_MICLIC_Segment?.graphic ?? GraphicDatabase.Get<Graphic_Single>("Things/Mote/SparkFlash", ShaderDatabase.MoteGlow);

            int count = nodes.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                float nodeLag = (float)(i + 1) / (count + 1);
                // 计算节点物理位置进度
                float nodeProg = Mathf.Clamp01(progress - nodeLag + nodes[i].extraInertia);

                Vector3 groundPos = Vector3.Lerp(origin, destination, nodeProg);
                nodes[i].lastGroundPos = groundPos;

                float currentH = 0f;
                // 计算节点实时高度
                if (nodeProg > 0f)
                {
                    // 获取理想高度
                    float idealH = (cachedArcHeightFactor ?? 2f) * GenMath.InverseParabola(nodeProg);
                    // 应用下垂系数修正高度
                    currentH = idealH * (1f - nodes[i].sagFactor);
                }

                Vector3 visualPos = groundPos;
                visualPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                visualPos.z += currentH;

                DrawBezierBetween(prev, visualPos, false);
                if (nodeProg > 0f) segmentGraphic.Draw(visualPos, Rotation, this, shotAngle);
                prev = visualPos;
            }

            DrawBezierBetween(prev, currentRocketPos, true);
        }

        private void DrawBezierBetween(Vector3 start, Vector3 end, bool isLeadSection)
        {
            float dist = (start - end).MagnitudeHorizontal();
            if (dist < 0.05f) return;

            float sag = isLeadSection ? Mathf.Clamp(dist * 0.005f, 0f, 0.08f) : Mathf.Clamp(dist * 0.015f, 0f, 0.12f);
            Vector3 p1 = Vector3.Lerp(start, end, 0.33f); p1.z -= sag;
            Vector3 p2 = Vector3.Lerp(start, end, 0.66f); p2.z -= sag;

            int subDivs = Mathf.Clamp(Mathf.RoundToInt(dist * 3f), 3, 12);
            Vector3 lastPt = start;
            for (int i = 1; i <= subDivs; i++)
            {
                float t = (float)i / subDivs;
                Vector3 currPt = CalculateBezierPoint(t, start, p1, p2, end);
                GenDraw.DrawLineBetween(lastPt, currPt, CableMat, 0.12f);
                lastPt = currPt;
            }
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t; float tt = t * t; float uu = u * u;
            return p0 * (uu * u) + 3f * p1 * (uu * t) + 3f * p2 * (u * tt) + p3 * (tt * t);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (isLanded) return;

            // 初始化着陆状态
            isLanded = true;
            impactPos = this.ExactPosition;
            landingTick = Find.TickManager.TicksGame;

            // 阻止弹体立即销毁
        }

        private void ExplodeAt(Vector3 pos, float angle)
        {
            IntVec3 cell = pos.ToIntVec3();
            if (Map == null || !cell.InBounds(Map)) return;

            float radius = def.projectile.explosionRadius;
            var damageWorker = DamageDefOf.Bomb.Worker;
            var allCells = damageWorker.ExplosionCellsToHit(cell, Map, radius);
            List<IntVec3> filtered = new List<IntVec3>();

            foreach (var c in allCells)
            {
                float cellAngle = cell.ToVector3Shifted().AngleToFlat(c.ToVector3Shifted());
                if (Mathf.Abs(Mathf.DeltaAngle(angle, cellAngle)) <= 90.5f) filtered.Add(c);
            }

            GenExplosion.DoExplosion(center: cell, map: Map, radius: radius, damType: DamageDefOf.Bomb,
                instigator: launcher, damAmount: def.projectile.GetDamageAmount(1f, null), direction: angle, overrideCells: filtered);
        }
    }
}
