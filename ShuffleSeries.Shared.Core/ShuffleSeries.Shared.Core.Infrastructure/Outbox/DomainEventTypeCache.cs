using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Infrastructure.Outbox;

/// <summary>
/// Thread-safe in-memory cache for resolving domain event types from assemblies.
/// Eliminates runtime reflection overhead and hardcoded magic strings across microservice Outbox processors.
/// </summary>
public static class DomainEventTypeCache
{
    private static readonly ConcurrentDictionary<Assembly, FrozenDictionary<string, Type>> _cache = new();

    /// <summary>
    /// Resolves the Type for a domain event name within the specified assembly.
    /// Uses FrozenDictionary for O(1) allocation-free lookups after initial assembly scan.
    /// </summary>
    /// <param name="domainAssembly">The assembly containing domain event definitions.</param>
    /// <param name="typeName">The short name of the domain event type (e.g. "UserRegisteredDomainEvent").</param>
    /// <returns>The matching Type if found and implements IDomainEvent; otherwise null.</returns>
    public static Type? Resolve(Assembly domainAssembly, string typeName)
    {
        ArgumentNullException.ThrowIfNull(domainAssembly);

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        var lookup = _cache.GetOrAdd(domainAssembly, static assembly =>
        {
            var dictionary = new Dictionary<string, Type>(StringComparer.Ordinal);
            var domainEventTypes = assembly.GetTypes()
                .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (var type in domainEventTypes)
            {
                // Register by short Name (e.g. "UserRegisteredDomainEvent")
                dictionary.TryAdd(type.Name, type);

                // Also register by FullName if available (e.g. "ShuffleSeries.Identity.Domain.Events.UserRegisteredDomainEvent")
                if (type.FullName is not null)
                {
                    dictionary.TryAdd(type.FullName, type);
                }
            }

            return dictionary.ToFrozenDictionary(StringComparer.Ordinal);
        });

        return lookup.GetValueOrDefault(typeName);
    }

    /// <summary>
    /// Clears the cached assembly lookups (useful for unit testing).
    /// </summary>
    public static void Clear() => _cache.Clear();
}
