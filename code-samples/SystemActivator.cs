using System;
using System.Linq;
using System.Reflection;

namespace Quantum.QuantumUser.Simulation.Common.Di
{
    public static class SystemActivator
    {
        public static T Create<T>(IGameplayDependencies deps) where T : class
        {
            return (T)Create(typeof(T), deps);
        }

        public static object Create(Type type, IGameplayDependencies deps)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length == 0)
                throw new InvalidOperationException($"Type {type.FullName} has no public constructors.");

            var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var pType = parameters[i].ParameterType;

                if (pType == typeof(IGameplayDependencies))
                {
                    args[i] = deps;
                    continue;
                }

                var resolved = deps.Resolve(pType);
                if (resolved == null)
                    throw new InvalidOperationException(
                        $"Cannot resolve constructor parameter '{parameters[i].Name}' of type " +
                        $"'{pType.FullName}' for system '{type.FullName}'. " +
                        $"Make sure it is registered in the host DI container.");

                args[i] = resolved;
            }

            return ctor.Invoke(args);
        }
    }
}