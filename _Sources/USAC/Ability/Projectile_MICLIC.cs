using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace USAC
{
    // 定义火箭排雷索弹体逻辑
    [StaticConstructorOnStartup]
    public class Projectile_MICLIC : Projectile_Explosive
    {
        private List<MineClearingLineSegment> segments = new List<MineClearingLineSegment>();
        private float? shotAngle = null;
        private float? cachedArcHeightFactor = null;
        private Vector3 lastSpawnPos = Vector3.zero;
        private float distanceAccumulator = 0f;
        private float spawnInterval = 0f;
        private const int MAX_SEGMENTS = 6;


        private List<List<IntVec3>> previewSegmentCells = new List<List<IntVec3>>();
        private bool isLanded = false; // 记录是否已落地
        private int landingTick = -1; // 记录落地时间戳
        private Vector3 impactPos; // 记录落地精确坐标

        protected override void Tick()
        {
            base.Tick();
            if (!this.Spawned) return;

            // 执行落地延迟起爆计时
            if (isLanded)
            {
                if (Find.TickManager.TicksGame >= landingTick + 30)
                {
                    DoFinalExplosion();
                }
                return;
            }

            Vector3 currentPos = this.DrawPos;

            // 初始化生成物理间隔
            if (lastSpawnPos == Vector3.zero)
            {
                lastSpawnPos = currentPos;
                float totalDist = (destination - origin).MagnitudeHorizontal();
                spawnInterval = totalDist / (MAX_SEGMENTS + 1);

                // 预计算爆炸预览范围
                PrecomputePreviewCells();
                return;
            }

            // 计算实时发射角度
            if (shotAngle == null)
            {
                shotAngle = origin.AngleToFlat(destination);
            }

            // 计算帧内水平位移
            Vector2 lastPos2D = new Vector2(lastSpawnPos.x, lastSpawnPos.z);
            Vector2 currentPos2D = new Vector2(currentPos.x, currentPos.z);
            float dist = Vector2.Distance(lastPos2D, currentPos2D);
            distanceAccumulator += dist;

            // 达到间隔阈值生成段
            if (distanceAccumulator >= spawnInterval && segments.Count < MAX_SEGMENTS)
            {
                distanceAccumulator -= spawnInterval;
                SpawnSegment(currentPos);
            }

            lastSpawnPos = currentPos;
        }

        private void PrecomputePreviewCells()
        {
            previewSegmentCells.Clear();
            float currentShotAngle = shotAngle ?? origin.AngleToFlat(destination);
            Vector3 direction = (destination - origin).normalized;

            for (int i = 1; i <= MAX_SEGMENTS; i++)
            {
                Vector3 pos = origin + direction * (spawnInterval * i);
                IntVec3 cell = pos.ToIntVec3();
                if (cell.InBounds(Map))
                {
                    previewSegmentCells.Add(CalculateExplosionCells(cell, currentShotAngle));
                }
            }

            // 添加火箭落点预起爆预览
            if (destination.ToIntVec3().InBounds(Map))
            {
                previewSegmentCells.Add(CalculateExplosionCells(destination.ToIntVec3(), currentShotAngle));
            }
        }

        private List<IntVec3> CalculateExplosionCells(IntVec3 center, float angle)
        {
            var damageWorker = DamageDefOf.Bomb.Worker;
            var allCells = damageWorker.ExplosionCellsToHit(center, Map, 5.9f, null, null, null);
            List<IntVec3> filtered = new List<IntVec3>();
            foreach (var cell in allCells)
            {
                float cellAngle = center.ToVector3Shifted().AngleToFlat(cell.ToVector3Shifted());
                if (Mathf.Abs(Mathf.DeltaAngle(angle, cellAngle)) <= 90.5f)
                {
                    filtered.Add(cell);
                }
            }
            return filtered;
        }

        private static readonly Material CableMat = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, new Color(0.15f, 0.15f, 0.15f));

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // 落地后使用固定落点进行绘制
            Vector3 finalLoc = isLanded ? impactPos : drawLoc;

            if (!isLanded)
            {
                // 计算实时飞行进度
                float totalDist = (destination - origin).MagnitudeHorizontal();
                float coveredDist = (ExactPosition - origin).MagnitudeHorizontal();
                float progress = totalDist > 0.01f ? Mathf.Clamp01(coveredDist / totalDist) : 0f;

                // 计算弧线高度因子
                if (cachedArcHeightFactor == null)
                {
                    float factor = def.projectile.arcHeightFactor;
                    float distSq = totalDist * totalDist;
                    if (factor * factor > distSq * 0.04f) factor = totalDist * 0.2f;
                    cachedArcHeightFactor = factor;
                }

                // 计算抛物线实时高度
                float arcHeight = cachedArcHeightFactor.Value * GenMath.InverseParabola(progress);

                // 应用视觉弧线高度
                finalLoc.z += arcHeight;
            }

            float currentShotAngle = shotAngle ?? origin.AngleToFlat(destination);
            Graphic.Draw(finalLoc, this.Rotation, this, currentShotAngle);

            // 绘制贝塞尔导线
            DrawConnectingCables(finalLoc);

            // 渲染飞行路径预览
            if (!isLanded) RenderFlightPreview();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (isLanded) return;

            // 初始化着陆状态，阻止立即销毁
            isLanded = true;
            landingTick = Find.TickManager.TicksGame;
            impactPos = this.ExactPosition;

            for (int i = 0; i < segments.Count; i++)
            {
                // 标记束端锚点位置并同步倒计时
                if (i == segments.Count - 1)
                {
                    segments[i].tailAnchorPos = impactPos;
                }
                segments[i].StartExplosionCountdown(30);
            }
        }

        private void DoFinalExplosion()
        {
            // 执行弹体自身同步爆炸，强制覆盖半径
            GenExplosion.DoExplosion(
                center: Position,
                map: Map,
                radius: 5.9f,
                damType: DamageDefOf.Bomb,
                instigator: launcher,
                damAmount: def.projectile.GetDamageAmount(launcher),
                applyDamageToExplosionCellsNeighbors: true,
                direction: shotAngle,
                overrideCells: CalculateExplosionCells(Position, shotAngle ?? 0f)
            );
            this.Destroy();
        }

        private void DrawConnectingCables(Vector3 currentRocketPos)
        {
            if (Map == null || !this.Spawned) return;

            // 绘制火箭至末端牵引线
            Vector3 lastPos = origin;
            if (segments.Any())
            {
                lastPos = segments.Last().VisualDrawPos;
            }

            // 执行导线绷紧绘制
            DrawLeadCable(lastPos, currentRocketPos);
        }

        private void DrawLeadCable(Vector3 start, Vector3 end)
        {
            float dist = (start - end).MagnitudeHorizontal();
            if (dist < 0.1f) return;

            // 调整牵引线偏移直度
            float sag = Mathf.Clamp(dist * 0.005f, 0.005f, 0.1f);

            Vector3 p1 = Vector3.Lerp(start, end, 0.33f);
            p1.z -= sag;
            Vector3 p2 = Vector3.Lerp(start, end, 0.66f);
            p2.z -= sag;

            int subDivs = Mathf.Clamp(Mathf.RoundToInt(dist * 2.5f), 4, 15);

            Vector3 prevPart = start;
            for (int i = 1; i <= subDivs; i++)
            {
                float t = (float)i / subDivs;
                Vector3 currentPart = CalculateBezierPoint(t, start, p1, p2, end);
                GenDraw.DrawLineBetween(prevPart, currentPart, CableMat, 0.08f);
                prevPart = currentPart;
            }
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            // 执行贝塞尔公式计算
            return p0 * uuu + 3f * p1 * uu * t + 3f * p2 * u * tt + p3 * ttt;
        }

        private void RenderFlightPreview()
        {
            if (Map == null) return;

            // 绘制预计轨迹预览
            GenDraw.DrawLineBetween(origin, destination, SimpleColor.White);

            // 绘制部署段落爆炸范围预览
            foreach (var cells in previewSegmentCells)
            {
                if (cells.Any())
                {
                    GenDraw.DrawFieldEdges(cells, Color.white);
                }
            }
        }

        private void SpawnSegment(Vector3 exactPos)
        {
            IntVec3 cell = exactPos.ToIntVec3();
            if (!cell.InBounds(Map)) return;

            float totalDist = (destination - origin).MagnitudeHorizontal();
            float coveredDist = (ExactPosition - origin).MagnitudeHorizontal();
            float progress = totalDist > 0.01f ? Mathf.Clamp01(coveredDist / totalDist) : 0f;

            if (cachedArcHeightFactor == null)
            {
                float factor = def.projectile.arcHeightFactor;
                float distSq = totalDist * totalDist;
                if (factor * factor > distSq * 0.04f) factor = totalDist * 0.2f;
                cachedArcHeightFactor = factor;
            }

            float arcHeight = cachedArcHeightFactor.Value * GenMath.InverseParabola(progress);

            MineClearingLineSegment segment = (MineClearingLineSegment)ThingMaker.MakeThing(USAC_DefOf.USAC_MICLIC_Segment);
            segment.shotAngle = this.shotAngle ?? 0f;

            // 构建索段链式连接
            if (!segments.Any()) segment.prevThing = launcher;
            else segment.prevThing = segments.Last();

            segment.exactSpawnPos = exactPos.Yto0();
            segment.spawnVisualHeight = arcHeight;

            GenSpawn.Spawn(segment, cell, Map);
            segments.Add(segment);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shotAngle, "shotAngle");
            Scribe_Values.Look(ref lastSpawnPos, "lastSpawnPos", Vector3.zero);
            Scribe_Values.Look(ref distanceAccumulator, "distanceAccumulator", 0f);
            Scribe_Values.Look(ref spawnInterval, "spawnInterval", 0f);
            Scribe_Values.Look(ref isLanded, "isLanded", false);
            Scribe_Values.Look(ref landingTick, "landingTick", -1);
            Scribe_Values.Look(ref impactPos, "impactPos", Vector3.zero);
        }
    }
}
