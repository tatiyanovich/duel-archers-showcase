using System;
using System.Collections.Generic;
using Quantum.QuantumUser.Simulation.Common.Di;

namespace Quantum.QuantumUser.Simulation.Common.Features
{
    /// <summary>
    /// Base class for every gameplay <c>SystemGroup</c> in the project.
    /// Concrete features list their child systems by calling <see cref="Add{T}"/> in the constructor body;
    /// dependencies of each system are auto-resolved from <see cref="IGameplayDependencies"/>
    /// (set by the Unity bootstrap via <see cref="GameplayDependenciesProvider"/>).
    /// <para>Example:</para>
    /// <code>
    /// public class WeaponFeature : FeatureBase
    /// {
    ///     public WeaponFeature() : base(nameof(WeaponFeature))
    ///     {
    ///         Add&lt;MarkBowReadyToFireSystem&gt;();
    ///         Add&lt;FireBowShotSystem&gt;();
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class FeatureBase : SystemGroup
    {
        private readonly IGameplayDependencies _deps;
        private readonly List<SystemBase> _children = new();

        protected FeatureBase(string name) : base(name, Array.Empty<SystemBase>())
        {
            _deps = GameplayDependenciesProvider.Require();
        }

        /// <summary>Activate a system, auto-injecting its dependencies, and append it as a child.</summary>
        protected void Add<T>() where T : SystemBase
        {
            var system = SystemActivator.Create<T>(_deps);
            AppendChild(system);
        }

        /// <summary>Append a pre-built system instance (escape hatch for special cases).</summary>
        protected void Add(SystemBase system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            AppendChild(system);
        }

        private void AppendChild(SystemBase system)
        {
            _children.Add(system);
            ChildSystems = _children;
        }
    }
}
