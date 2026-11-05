namespace MerryCountdown.Core.Infrastructure;

public interface IAppUpdater
{
    Task EnsureAppUpToDateAsync();
}
