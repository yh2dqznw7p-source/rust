// SPDX-License-Identifier: MIT
// RustLike — minimal Service Locator.
//
// Rationale:
//   - We avoid a heavy DI container (Zenject/VContainer) on the server hot path.
//   - All services register at boot and never change at runtime, so a flat
//     dictionary keyed by Type is enough and is allocation-free after warmup.
//   - For tests, services can be replaced via Replace<T>(...).
//
// Lifecycle:
//   ServiceLocator is created by Bootstrap, populated, then `Lock()`-ed.
//   After Lock(), Register<T> throws — only Replace<T> is allowed (tests).

using System;
using System.Collections.Generic;
using RustLike.Core.Logging;

namespace RustLike.Core.Bootstrap
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> s_Services = new(64);
        private static bool s_Locked;

        public static void Reset()
        {
            s_Services.Clear();
            s_Locked = false;
        }

        public static void Lock() => s_Locked = true;

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (s_Locked) throw new InvalidOperationException(
                $"ServiceLocator is locked. Use Replace<{typeof(T).Name}> for tests only.");
            s_Services[typeof(T)] = service;
            Log.Info(LogCat.Boot, "Registered: ", typeof(T).Name);
        }

        public static void Replace<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            s_Services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out var svc))
                return (T)svc;
            throw new InvalidOperationException("Service not registered: " + typeof(T).Name);
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out var svc))
            {
                service = (T)svc;
                return true;
            }
            service = null;
            return false;
        }
    }
}
