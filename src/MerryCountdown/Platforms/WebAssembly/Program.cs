using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new MerryCountdown.App())
    .UseWebAssembly()
    .Build();

await host.RunAsync();
