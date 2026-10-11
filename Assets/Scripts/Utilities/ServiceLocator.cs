using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rival.Utilities
{
    /// <summary>
    /// Minimal dependency-injection registry. Systems register themselves under
    /// an interface; consumers request the interface. This breaks hard compile-time
    /// coupling between systems and keeps gameplay code testable and swappable.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null)
            {
                Debug.LogError($"[ServiceLocator] Refusing to register a null {typeof(T).Name}.");
                return;
            }

            if (services.TryGetValue(typeof(T), out _))
            {
                Debug.LogWarning($"[ServiceLocator] {typeof(T).Name} is already registered; overwriting.");
            }

            services[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var instance) && instance is T typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"[ServiceLocator] No service registered for {typeof(T).Name}.");
        }

        public static bool TryGet<T>(out T instance) where T : class
        {
            if (services.TryGetValue(typeof(T), out var value) && value is T typed)
            {
                instance = typed;
                return true;
            }

            instance = null;
            return false;
        }

        public static void Unregister<T>(T instance) where T : class
        {
            if (services.TryGetValue(typeof(T), out var existing) && ReferenceEquals(existing, instance))
            {
                services.Remove(typeof(T));
            }
        }

        public static void Clear() => services.Clear();
    }
}
