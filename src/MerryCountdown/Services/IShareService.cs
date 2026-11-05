namespace MerryCountdown.Services;

public interface IShareService
{
    Task ShareAsync(string title, string uri);
}
