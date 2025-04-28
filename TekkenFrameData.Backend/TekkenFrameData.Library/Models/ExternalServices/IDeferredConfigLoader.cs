using TwitchLib.Client;

namespace TekkenFrameData.Library.Models.ExternalServices;

public interface IDeferredConfigLoader<T>
    where T : class
{
    protected bool IsActive { get; set; }
    protected T? Service { get; set; }
    protected bool ThrowOnServiceNull { get; set; }
    protected void CreateService(Func<T> factory)
    {
        Service = factory();
    }

    protected void CreateService(T service)
    {
        Service = service;
    }

    protected void EnsureServiceInitialized()
    {
        if (Service == null)
        {
            throw new NullReferenceException(typeof(T).Name + " was null");
        }
    }

    // Делегируем все вызовы в Service, если он не null
    TA? CallService<TA>(Func<T, TA> func) => Service is null ? default : func(Service);

    void CallService(Action<T> action)
    {
        EnsureServiceInitialized();
        action(Service!);
    }
}
