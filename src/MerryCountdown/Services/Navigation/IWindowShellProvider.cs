using MerryCountdown.Core.ViewModels;
using MerryCountdown.Infrastructure;
using Microsoft.UI.Dispatching;

namespace MerryCountdown.Services.Navigation;

public interface IWindowShellProvider
{
    Window Window { get; }

    IWindowShell Shell { get; }

    WindowShellViewModel ViewModel { get; }

    DispatcherQueue DispatcherQueue { get; }

    Frame RootFrame { get; }
}
