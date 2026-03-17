using System;

namespace WukongMp.Api;

public interface IDependencyContainer
{
    void RegisterSingleton<TService>();
    void RegisterSingleton<TService>(TService instance);
    void RegisterSingleton<TService>(Type implementationType);
    void RegisterSingleton<TService, TImplementation>() where TImplementation : TService;
    void RegisterSingleton<TService, TImplementation>(TImplementation instance) where TImplementation : TService;   
    T Resolve<T>();
}