using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum.QuantumUser.Simulation.Gameplay.Features.Ultimate.Systems
{
    /// <summary>Accumulates ultimate charge over time and notifies the HUD on integer-percent changes.</summary>
    [Preserve]
    public unsafe class UltimateChargeSystem : SystemMainThreadFilter<UltimateChargeSystem.Filter>
    {
        public override void Update(Frame f, ref Filter filter)
        {
            var oldValue = filter.Points->Value;
            var newValue = FPMath.Min(oldValue + UltimateConstants.PointsPerTick, UltimateConstants.MaxPoints);

            if (newValue == oldValue)
                return;

            filter.Points->Value = newValue;
            EmitProgressIfPercentChanged(f, filter.Entity, oldValue, newValue);
        }


        private static void EmitProgressIfPercentChanged(Frame f, EntityRef entity, FP oldValue, FP newValue)
        {
            var oldPercent = ToPercent(oldValue);
            var newPercent = ToPercent(newValue);
            if (oldPercent == newPercent)
                return;

            f.Events.UltimatePointsChanged(entity, newValue / UltimateConstants.MaxPoints);
        }

        private static int ToPercent(FP value)
        {
            return (value * 100 / UltimateConstants.MaxPoints).AsInt;
        }

        public struct Filter
        {
            public EntityRef Entity;
            public PlayerLink* PlayerLink;
            public UltimatePoints* Points;
        }
    }
}