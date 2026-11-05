using System.Runtime.InteropServices;
using MerryCountdown.Services.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace MerryCountdown.Services;

public sealed class ShareService : IShareService
{
    private readonly IWindowShellProvider _windowShellProvider;
    private string? _pendingTitle;
    private string? _pendingUri;
    private string? _pendingText;

    public ShareService(IWindowShellProvider windowShellProvider)
        => _windowShellProvider = windowShellProvider;

    public Task ShareAsync(string title, string uri)
    {
        _pendingUri = uri;
        return ShowShareUI(title);
    }

    public Task ShareTextAsync(string title, string text)
    {
        _pendingText = text;
        return ShowShareUI(title);
    }

    private Task ShowShareUI(string title)
    {
        _pendingTitle = title;

#if !HAS_UNO
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_windowShellProvider.Window);
        var interop = DataTransferManager.As<IDataTransferManagerInterop>();
        var result = interop.GetForWindow(hwnd, _dtmIid);
        var dtm = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
        dtm.DataRequested += OnDataRequested;
        try
        {
            interop.ShowShareUIForWindow(hwnd);
        }
        catch (COMException)
        {
            dtm.DataRequested -= OnDataRequested;
        }
#else
		var dtm = DataTransferManager.GetForCurrentView();
		dtm.DataRequested += OnDataRequested;
		try
		{
			DataTransferManager.ShowShareUI();
		}
		catch (COMException)
		{
			dtm.DataRequested -= OnDataRequested;
		}
#endif

        return Task.CompletedTask;
    }

    private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        sender.DataRequested -= OnDataRequested;
        args.Request.Data.Properties.Title = _pendingTitle ?? string.Empty;
        if (_pendingUri is not null)
        {
            args.Request.Data.SetWebLink(new Uri(_pendingUri));
            args.Request.Data.SetText(_pendingUri);
        }
        else if (_pendingText is not null)
        {
            args.Request.Data.SetText(_pendingText);
        }

        _pendingTitle = null;
        _pendingUri = null;
        _pendingText = null;
    }

#if !HAS_UNO
    [ComImport]
    [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);

        void ShowShareUIForWindow(IntPtr appWindow);
    }

    private static readonly Guid _dtmIid =
        new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);
#endif
}
