using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple dependency injection container for economic services.
/// Provides service registration and resolution for the economic engine.
/// </summary>
public class EconomicServiceContainer
{
    private static EconomicServiceContainer _instance;
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    public static EconomicServiceContainer Instance
    {
        get
        {
            if (_instance == null)
                _instance = new EconomicServiceContainer();
            return _instance;
        }
    }

    /// <summary>
    /// Registers a singleton service instance
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <param name="implementation">Service implementation</param>
    public void RegisterSingleton<T>(T implementation)
    {
        if (implementation == null)
            throw new ArgumentNullException(nameof(implementation));

        _services[typeof(T)] = implementation;
    }

    /// <summary>
    /// Registers a factory for creating service instances
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <param name="factory">Factory function</param>
    public void RegisterFactory<T>(Func<T> factory)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _factories[typeof(T)] = () => factory();
    }

    /// <summary>
    /// Resolves a service instance
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>Service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
    public T Resolve<T>()
    {
        var serviceType = typeof(T);

        // Check for singleton instance first
        if (_services.TryGetValue(serviceType, out var service))
        {
            return (T)service;
        }

        // Check for factory
        if (_factories.TryGetValue(serviceType, out var factory))
        {
            return (T)factory();
        }

        throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
    }

    /// <summary>
    /// Checks if a service is registered
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>True if service is registered</returns>
    public bool IsRegistered<T>()
    {
        var serviceType = typeof(T);
        return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
    }

    /// <summary>
    /// Clears all registered services (useful for testing)
    /// </summary>
    public void Clear()
    {
        _services.Clear();
        _factories.Clear();
    }

    /// <summary>
    /// Configures default services for the economic engine
    /// </summary>
    public void ConfigureDefaults()
    {
        // Register pure C# implementation
        RegisterSingleton<IEconomicEngine>(new EconomicEngine());
        
        // Register trade logger factory
        RegisterFactory<ITradeLogger>(() => new TradeLoggerV1());
        
        Debug.Log("Economic Service Container configured with defaults");
    }

    /// <summary>
    /// Configures services for testing with mock implementations
    /// </summary>
    public void ConfigureForTesting()
    {
        Clear();
        
        // Register test implementations
        RegisterSingleton<IEconomicEngine>(new EconomicEngine());
        RegisterSingleton<ITradeLogger>(new MockLogger());
        
        Debug.Log("Economic Service Container configured for testing");
    }
}