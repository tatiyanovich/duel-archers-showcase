using Photon.Deterministic;
using Quantum.QuantumUser.Simulation.Common.Extensions;
using Quantum.QuantumUser.Simulation.Gameplay.Common.Layers;
using Quantum.QuantumUser.Simulation.Gameplay.Features.Ultimate.Configs;
using Quantum.QuantumUser.Simulation.Gameplay.StaticData.Configs;
using UnityEngine.Scripting;

namespace Quantum.QuantumUser.Simulation.Gameplay.Features.Ultimate.Systems
{
    /// <summary>
    ///     Advances dashing heroes substep-by-substep with collision-aware sweeps and finalizes the dash when blocked or
    ///     distance/time runs out.
    /// </summary>
    [Preserve]
    public unsafe class DashUltimateMoveSystem : SystemMainThreadFilter<DashUltimateMoveSystem.Filter>
    {
        private readonly IUltimateConfigs _ultimateConfigs;

        public DashUltimateMoveSystem(IUltimateConfigs ultimateConfigs)
        {
            _ultimateConfigs = ultimateConfigs;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            if (IsDashFinished(filter))
            {
                FinishDash(f, ref filter);
                return;
            }

            var dashLevel = GetDashLevel();
            var deltaTime = f.DeltaTime;
            var stepDistance = ClampStep(filter.Speed->Value * deltaTime, filter.RemainingDistance->Value);
            var substepDistance = stepDistance / (FP)dashLevel.Substeps;
            var direction = filter.Direction->Value.AsHorizontalOrForward();
            var obstacleMask = LayerMasks.DashObstacles(f);

            var endPosition = filter.Transform->Position;
            var movedThisTick = FP._0;
            var blocked = false;

            for (var i = 0; i < dashLevel.Substeps; i++)
            {
                if (substepDistance <= FP._0 || filter.RemainingDistance->Value <= FP._0) break;

                var advance = SweepSubstep(f, endPosition, direction, substepDistance, obstacleMask,
                    dashLevel.SubstepSkin);
                if (advance <= FP.Epsilon)
                {
                    blocked = true;
                    break;
                }

                endPosition += direction * advance;
                movedThisTick += advance;
                filter.RemainingDistance->Value -= advance;
            }

            filter.Transform->Position = endPosition;
            filter.Dashing->TimeLeft = FPMath.Max(FP._0, filter.Dashing->TimeLeft - deltaTime);

            if (blocked || ShouldFinish(filter, movedThisTick)) FinishDash(f, ref filter);
        }

        private DashUltimateLevel GetDashLevel()
        {
            return _ultimateConfigs.GetConfig<DashUltimateConfig>(EUltimateId.Dash)
                .GetLevel(WeaponLevels.DefaultLevel);
        }

        private static bool IsDashFinished(Filter filter)
        {
            return filter.RemainingDistance->Value <= FP._0 || filter.Dashing->TimeLeft <= FP._0;
        }

        private static bool ShouldFinish(Filter filter, FP movedThisTick)
        {
            return movedThisTick <= FP.Epsilon
                   || filter.RemainingDistance->Value <= FP._0
                   || filter.Dashing->TimeLeft <= FP._0;
        }

        private static FP ClampStep(FP step, FP maxRemaining)
        {
            if (step > maxRemaining) return maxRemaining;

            return step;
        }

        private static FP SweepSubstep(Frame f, FPVector3 origin, FPVector3 direction, FP substepDistance,
            int obstacleMask, FP substepSkin)
        {
            var hit = f.Physics3D.Raycast(origin, direction, substepDistance, obstacleMask);
            if (!hit.HasValue) return substepDistance;

            return FPMath.Max(FP._0, hit.Value.CastDistanceNormalized * substepDistance - substepSkin);
        }

        private static void FinishDash(Frame f, ref Filter filter)
        {
            f.Events.DashEnded(filter.Entity);
            f.Remove<Dashing>(filter.Entity);
            f.Remove<DashSpeed>(filter.Entity);
            f.Remove<DashRemainingDistance>(filter.Entity);
        }

        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public Dashing* Dashing;
            public DashSpeed* Speed;
            public DashRemainingDistance* RemainingDistance;
            public DashDirection* Direction;
        }
    }
}