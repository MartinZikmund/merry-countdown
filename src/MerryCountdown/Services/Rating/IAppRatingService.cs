namespace MerryCountdown.Services.Rating;

public interface IAppRatingService
{
    Task TryPromptForRatingAsync();
}
