using MerryCountdown.Core.Services;
using Microsoft.UI.Dispatching;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;

namespace MerryCountdown.Services;

/// <summary>AudioGraph: every effect is decoded and wired to the speakers up front, so a tap only has to start a node.</summary>
public sealed class PlatformSoundOutput : ISoundOutput
{
    // A running graph holds the audio device open and keeps the PC from sleeping, so it only runs around a sound.
    private static readonly TimeSpan _idleAfter = TimeSpan.FromSeconds(4);

    private readonly ILogger<PlatformSoundOutput> _logger;
    private readonly Dictionary<SoundEffect, AudioFileInputNode> _nodes = [];
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly DispatcherQueueTimer _idleTimer;
    private AudioGraph? _graph;
    private Task? _loading;
    private bool _isRunning;

    public PlatformSoundOutput(ILogger<PlatformSoundOutput> logger)
    {
        _logger = logger;

        _idleTimer = _dispatcher.CreateTimer();
        _idleTimer.Interval = _idleAfter;
        _idleTimer.IsRepeating = false;
        _idleTimer.Tick += (_, _) =>
        {
            _graph?.Stop();
            _isRunning = false;
        };

        _loading = LoadAsync();
    }

    public void Play(SoundEffect effect)
    {
        // A missing jingle must never take the app down with it.
        try
        {
            // The device went away, say unplugged headphones: build a new graph for the next tap.
            _loading ??= LoadAsync();

            if (_graph is null || !_nodes.TryGetValue(effect, out var node))
            {
                return;
            }

            if (!_isRunning)
            {
                _graph.Start();
                _isRunning = true;
            }

            node.Seek(TimeSpan.Zero);
            node.Start();

            // Restarts the wait, so the graph outlives the last sound rather than the first.
            _idleTimer.Stop();
            _idleTimer.Start();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't play {Effect}", effect);
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            AudioGraphSettings settings = new(AudioRenderCategory.SoundEffects)
            {
                QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency,
            };

            var created = await AudioGraph.CreateAsync(settings);
            if (created.Status != AudioGraphCreationStatus.Success)
            {
                _logger.LogWarning("No audio graph: {Status}", created.Status);
                return;
            }

            var graph = created.Graph;
            var speakers = await graph.CreateDeviceOutputNodeAsync();
            if (speakers.Status != AudioDeviceNodeCreationStatus.Success)
            {
                _logger.LogWarning("No audio output: {Status}", speakers.Status);
                graph.Dispose();
                return;
            }

            foreach (var effect in Enum.GetValues<SoundEffect>())
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Sounds/{SoundFiles.NameOf(effect)}"));
                var input = await graph.CreateFileInputNodeAsync(file);
                if (input.Status != AudioFileNodeCreationStatus.Success)
                {
                    _logger.LogWarning("Couldn't load {Effect}: {Status}", effect, input.Status);
                    continue;
                }

                // File nodes start themselves with the graph unless told otherwise.
                input.FileInputNode.Stop();
                input.FileInputNode.AddOutgoingConnection(speakers.DeviceOutputNode);
                _nodes[effect] = input.FileInputNode;
            }

            graph.UnrecoverableErrorOccurred += OnGraphLost;
            _graph = graph;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't set up the sounds");
        }
    }

    // Raised off the UI thread, while a tap may be reading the same fields.
    private void OnGraphLost(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
    {
        _logger.LogWarning("Audio graph lost: {Error}", args.Error);
        _dispatcher.TryEnqueue(() =>
        {
            sender.UnrecoverableErrorOccurred -= OnGraphLost;
            _idleTimer.Stop();
            _graph = null;
            _isRunning = false;
            _nodes.Clear();
            _loading = null;
            sender.Dispose();
        });
    }
}
