using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using ShuffleSeries.Shared.Core.Domain.Attributes;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Serilog destructuring policy that automatically masks sensitive fields (passwords, tokens, secrets, PII)
/// during asynchronous log serialization, offloading all masking overhead from the main application thread.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly ConcurrentDictionary<Type, PropertyDestructureMetadata[]> _propertyCache = new();

    private static readonly string[] _sensitiveKeywords =
    [
        "password",
        "token",
        "secret",
        "authorization",
        "apikey",
        "api_key",
        "creditcard",
        "cardnumber",
        "cvv",
        "ssn",
        "privatekey"
    ];

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        if (value is null)
        {
            result = null;
            return false;
        }

        var type = value.GetType();

        if (IsScalarOrSystemType(type, value))
        {
            result = null;
            return false;
        }

        var properties = _propertyCache.GetOrAdd(type, CreatePropertyMetadata);
        var logProperties = new List<LogEventProperty>(properties.Length);

        foreach (var prop in properties)
        {
            logProperties.Add(BuildLogProperty(prop, value, propertyValueFactory));
        }

        result = new StructureValue(logProperties, type.Name);
        return true;
    }

    private static bool IsScalarOrSystemType(Type type, object value)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            return true;

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri))
            return true;

        if (typeof(Type).IsAssignableFrom(type) || typeof(MemberInfo).IsAssignableFrom(type))
            return true;

        if (typeof(Delegate).IsAssignableFrom(type) || typeof(Stream).IsAssignableFrom(type) || type == typeof(CancellationToken))
            return true;

        return value is IEnumerable;
    }

    private static LogEventProperty BuildLogProperty(
        PropertyDestructureMetadata prop,
        object target,
        ILogEventPropertyValueFactory propertyValueFactory)
    {
        if (prop.IsSensitive)
        {
            return new LogEventProperty(prop.Name, new ScalarValue(prop.MaskValue));
        }

        object? rawValue;
        try
        {
            rawValue = prop.PropertyInfo.GetValue(target);
        }
        catch
        {
            rawValue = "<error reading property>";
        }

        var propValue = propertyValueFactory.CreatePropertyValue(rawValue, destructureObjects: true);
        return new LogEventProperty(prop.Name, propValue);
    }

    private static PropertyDestructureMetadata[] CreatePropertyMetadata(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        var list = new List<PropertyDestructureMetadata>();

        foreach (var prop in properties)
        {
            var maskAttr = prop.GetCustomAttribute<MaskSensitiveDataAttribute>();
            if (maskAttr is not null)
            {
                list.Add(new PropertyDestructureMetadata(prop, prop.Name, IsSensitive: true, maskAttr.Mask));
                continue;
            }

            var isKeywordSensitive = MatchesSensitiveKeyword(prop.Name);
            list.Add(new PropertyDestructureMetadata(
                prop,
                prop.Name,
                IsSensitive: isKeywordSensitive,
                MaskValue: isKeywordSensitive ? "***MASKED***" : string.Empty));
        }

        return [.. list];
    }

    private static bool MatchesSensitiveKeyword(string propertyName) =>
        _sensitiveKeywords.Any(k => propertyName.Contains(k, StringComparison.OrdinalIgnoreCase));

    private sealed record PropertyDestructureMetadata(
        PropertyInfo PropertyInfo,
        string Name,
        bool IsSensitive,
        string MaskValue
    );
}
