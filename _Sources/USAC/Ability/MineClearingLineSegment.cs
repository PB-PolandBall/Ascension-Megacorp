using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace USAC
{
    // 定义排雷索分段逻辑
    [StaticConstructorOnStartup]
    public class MineClearingLineSegment : Thing
    {
        private int explosionTicks = -1;
        public Thing prevThing = null;
        public float externalTension = 0f; // 记录外部张力系数
        public Vector3 lastPointStatic = Vector3.zero;
        private bool isTaut = false;
        private Vector3 targetTautPos = Vector3.zero; // 记录绷直目标位置

        public float shotAngle = 0f;
        public float spawnVisualHeight = 0f;
        public Vector3 exactSpawnPos = Vector3.zero;
        private float currentVisualHeight = 0f;
        private int ageTicks = 0;
        private const int FALL_DURATION = 30;
        private List<IntVec3> cachedCells = null;

        public Vector3 VisualDrawPos
        {
            get
            {
                Vector3 pos = exactSpawnPos;
                pos.z += currentVisualHeight;
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                return pos;
            }
        }

        public bool IsFalling => ageTicks < FALL_DURATION;

        public void StartExplosionCountdown(int ticks)
        {
            explosionTicks = ticks;
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                currentVisualHeight = spawnVisualHeight;
            }
        }

        protected override void Tick()
        {
            base.Tick();

            ageTicks++;
            if (ageTicks <= FALL_DURATION)
            {
                float t = (float)ageTicks / FALL_DURATION;
                currentVisualHeight = spawnVisualHeight * (1f - t * t);
            }
            else
            {
                currentVisualHeight = 0f;
            }

            if (explosionTicks > 0)
            {
                explosionTicks--;
                if (explosionTicks == 0) DoExplosion();
            }
        }

        public Vector3? tailAnchorPos = null; // 记录末端锚点坐标

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 currentPos = VisualDrawPos;
            Vector3 connectTo = lastPointStatic;

            if (prevThing != null && prevThing.Spawned)
            {
                // 实时追踪前驱坐标
                if (prevThing is MineClearingLineSegment other) connectTo = other.VisualDrawPos;
                else connectTo = prevThing.DrawPos;
            }

            if (connectTo != Vector3.zero)
            {
                DrawBezierToPoint(connectTo, currentPos);
            }

            // 绘制末端锚点连线
            if (tailAnchorPos.HasValue)
            {
                DrawBezierToPoint(currentPos, tailAnchorPos.Value);
            }

            Graphic.Draw(currentPos, Rotation, this, shotAngle);
        }

        private static readonly Material CableMat = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, new Color(0.15f, 0.15f, 0.15f));

        private void DrawBezierToPoint(Vector3 start, Vector3 end)
        {
            float dist = (start - end).MagnitudeHorizontal();
            if (dist < 0.1f) return;

            // 调整下垂系数硬度
            // 定义基础下垂系数值
            float baseSag = IsFalling ? 0.01f : 0.03f;
            float targetSag = 0.002f;

            float sagFactor = Mathf.Lerp(baseSag, targetSag, externalTension);
            float sag = Mathf.Clamp(dist * sagFactor, 0.001f, 0.2f);

            Vector3 p1 = Vector3.Lerp(start, end, 0.33f);
            p1.z -= sag;
            Vector3 p2 = Vector3.Lerp(start, end, 0.66f);
            p2.z -= sag;

            int subDivs = Mathf.Clamp(Mathf.RoundToInt(dist * 2.5f), 4, 12);
            Vector3 prev = start;
            for (int i = 1; i <= subDivs; i++)
            {
                float t = (float)i / subDivs;
                Vector3 curr = CalculateBezierPoint(t, start, p1, p2, end);
                GenDraw.DrawLineBetween(prev, curr, CableMat, 0.08f);
                prev = curr;
            }
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            return p0 * uuu + 3f * p1 * uu * t + 3f * p2 * u * tt + p3 * ttt;
        }

        private List<IntVec3> GetExplosionCells()
        {
            if (cachedCells == null)
            {
                var damageWorker = DamageDefOf.Bomb.Worker;
                // 同步计算半径至爆炸范围
                var allCells = damageWorker.ExplosionCellsToHit(Position, Map, 5.9f, null, null, null);
                cachedCells = new List<IntVec3>();
                foreach (var cell in allCells)
                {
                    float cellAngle = Position.ToVector3Shifted().AngleToFlat(cell.ToVector3Shifted());
                    if (Mathf.Abs(Mathf.DeltaAngle(shotAngle, cellAngle)) <= 90.5f) cachedCells.Add(cell);
                }
            }
            return cachedCells;
        }

        public void DoExplosion()
        {
            if (!Spawned) return;
            GenExplosion.DoExplosion(
                center: Position,
                map: Map,
                radius: 5.9f, // 增加半径确保覆盖范围
                damType: DamageDefOf.Bomb,
                instigator: this,
                damAmount: 50,
                applyDamageToExplosionCellsNeighbors: true,
                direction: shotAngle,
                overrideCells: GetExplosionCells()
            );
            this.Destroy();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref explosionTicks, "explosionTicks", -1);
            Scribe_Values.Look(ref shotAngle, "shotAngle", 0f);
            Scribe_Values.Look(ref exactSpawnPos, "exactSpawnPos", Vector3.zero);
            Scribe_Values.Look(ref targetTautPos, "targetTautPos", Vector3.zero);
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref isTaut, "isTaut", false);
            Scribe_Values.Look(ref tailAnchorPos, "tailAnchorPos", null);
            Scribe_References.Look(ref prevThing, "prevThing");
        }
    }
}
