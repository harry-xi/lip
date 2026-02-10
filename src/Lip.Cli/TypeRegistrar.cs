using Spectre.Console.Cli;
using System.Collections;

namespace Lip.Cli;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly Dictionary<Type, List<Type>> _registrations = [];
    private readonly Dictionary<Type, object> _instances = [];
    private readonly Dictionary<Type, Func<object>> _factories = [];

    public void Register(Type service, Type implementation)
    {
        if (!_registrations.TryGetValue(service, out var list))
        {
            list = [];
            _registrations[service] = list;
        }
        list.Add(implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _instances[service] = implementation;
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _factories[service] = factory;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_registrations, _instances, _factories);
    }
}

public sealed class TypeResolver(
    Dictionary<Type, List<Type>> registrations,
    Dictionary<Type, object> instances,
    Dictionary<Type, Func<object>> factories) : ITypeResolver, IDisposable
{
    private readonly Dictionary<Type, List<Type>> _registrations = registrations;
    private readonly Dictionary<Type, object> _instances = instances;
    private readonly Dictionary<Type, Func<object>> _factories = factories;

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        // 1. Check instances
        if (_instances.TryGetValue(type, out var instance))
        {
            return instance;
        }

        // 2. Check factories
        if (_factories.TryGetValue(type, out var factory))
        {
            instance = factory();
            _instances[type] = instance; // Cache singleton behavior for factories
            return instance;
        }

        // 3. Handle IEnumerable<T>
        if (type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var itemType = type.GetGenericArguments()[0];
            if (_registrations.TryGetValue(itemType, out var implementationTypes))
            {
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = (IList)Activator.CreateInstance(listType)!;
                foreach (var implType in implementationTypes)
                {
                    list.Add(ResolveType(implType));
                }
                return list;
            }
            return Array.CreateInstance(itemType, 0);
        }

        // 4. Check registrations
        if (_registrations.TryGetValue(type, out var implementations) && implementations.Count > 0)
        {
            // Resolve the last registered implementation
            return ResolveType(implementations[^1]);
        }

        // 5. Try to resolve concrete types (like Commands) directly
        if (!type.IsAbstract && !type.IsInterface)
        {
            return ResolveType(type);
        }

        return null;
    }

    private object ResolveType(Type type)
    {
        var constructor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor == null)
        {
            return Activator.CreateInstance(type)!;
        }

        var parameters = constructor.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = Resolve(parameters[i].ParameterType);
        }

        return constructor.Invoke(args);
    }

    public void Dispose()
    {
        foreach (var instance in _instances.Values.OfType<IDisposable>())
        {
            instance.Dispose();
        }
    }
}