namespace MerryCountdown.Services.Dialogs;

public interface IDialogCoordinator
{
    Task<ContentDialogResult> ShowAsync(ContentDialog dialog);
}
