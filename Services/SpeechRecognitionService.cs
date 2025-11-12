using Azure.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace meeting_copilot.Services;

/// <summary>
/// Provides conversation transcription with speaker diarization and automatic recovery.
/// </summary>
public sealed class SpeechRecognitionService : IDisposable
{
    private readonly string _conversationEndpoint;
    private readonly string _region;
    private readonly DefaultAzureCredential _credential;
    private readonly string? _subscriptionKey;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly ConcurrentBag<TranscriptionResult> _results = new();

    private ConversationTranscriber? _transcriber;
    private CancellationTokenSource? _activeCancellation;
    private bool _isStopping;

    public event EventHandler<TranscriptionResult>? OnTranscribing;
    public event EventHandler<TranscriptionResult>? OnTranscribed;
    public event EventHandler<string>? OnError;
    public event EventHandler<string>? OnStatus;

    public SpeechRecognitionService(IConfiguration configuration)
    {
        var endpointFromConfig = configuration["AzureSpeech:Endpoint"] ??
            Environment.GetEnvironmentVariable("AZURE_SPEECH_ENDPOINT") ??
            Environment.GetEnvironmentVariable("AZURESPEECH_ENDPOINT") ??
            Environment.GetEnvironmentVariable("ENDPOINT");

        var regionFromConfig = configuration["AzureSpeech:Region"] ??
            Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ??
            Environment.GetEnvironmentVariable("AZURESPEECH_REGION");

        if (!string.IsNullOrWhiteSpace(endpointFromConfig))
        {
            var normalizedEndpoint = NormalizeEndpoint(endpointFromConfig);
            _conversationEndpoint = BuildConversationEndpoint(normalizedEndpoint);
            _region = regionFromConfig ?? TryExtractRegionFromEndpoint(normalizedEndpoint) ?? "eastus2";
        }
        else
        {
            _region = regionFromConfig ?? "eastus2";
            var defaultEndpoint = BuildDefaultEndpoint(_region);
            _conversationEndpoint = BuildConversationEndpoint(defaultEndpoint);
        }

        _subscriptionKey = configuration["AzureSpeech:SubscriptionKey"] ??
            configuration["AzureSpeech:SpeechKey"] ??
            Environment.GetEnvironmentVariable("SPEECH_KEY") ??
            configuration["AZURE_SPEECH_KEY"] ??
            Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");

        _credential = new DefaultAzureCredential();
    }

    public async Task RecognizeFromMicrophoneAsync(CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopRecognitionInternalAsync().ConfigureAwait(false);

            _activeCancellation?.Dispose();
            _activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isStopping = false;

            await StartTranscriberAsync(_activeCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task StopRecognitionAsync()
    {
        await _stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _isStopping = true;
            _activeCancellation?.Cancel();
            await StopRecognitionInternalAsync().ConfigureAwait(false);
            OnStatus?.Invoke(this, "Transcription stopped.");
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public IEnumerable<TranscriptionResult> GetResults() => _results.ToArray();

    public void ClearResults() => _results.Clear();

    public void Dispose()
    {
        try
        {
            StopRecognitionAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // best effort during shutdown
        }
        finally
        {
            _activeCancellation?.Dispose();
            _stateLock.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private async Task StartTranscriberAsync(CancellationToken token)
    {
        var speechConfig = CreateSpeechConfig();
        ConfigureSpeechConfig(speechConfig);

        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        var transcriber = new ConversationTranscriber(speechConfig, audioConfig);
        _transcriber = transcriber;

        transcriber.Transcribing += ConversationTranscriber_Transcribing;
        transcriber.Transcribed += ConversationTranscriber_Transcribed;
        transcriber.Canceled += ConversationTranscriber_Canceled;
        transcriber.SessionStarted += ConversationTranscriber_SessionStarted;
        transcriber.SessionStopped += ConversationTranscriber_SessionStopped;

        token.Register(() => _ = Task.Run(StopRecognitionAsync));

        await transcriber.StartTranscribingAsync().ConfigureAwait(false);
    }

    private async Task StopRecognitionInternalAsync()
    {
        var transcriber = _transcriber;
        if (transcriber == null)
        {
            return;
        }

        _transcriber = null;

        transcriber.Transcribing -= ConversationTranscriber_Transcribing;
        transcriber.Transcribed -= ConversationTranscriber_Transcribed;
        transcriber.Canceled -= ConversationTranscriber_Canceled;
        transcriber.SessionStarted -= ConversationTranscriber_SessionStarted;
        transcriber.SessionStopped -= ConversationTranscriber_SessionStopped;

        try
        {
            await transcriber.StopTranscribingAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Stop failed: {ex.Message}");
        }
        finally
        {
            transcriber.Dispose();
        }

        _activeCancellation?.Dispose();
        _activeCancellation = null;
    }

    private void ConversationTranscriber_SessionStarted(object? sender, SessionEventArgs e)
    {
        OnStatus?.Invoke(this, "Transcription session started.");
    }

    private void ConversationTranscriber_Transcribing(object? sender, ConversationTranscriptionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Result.Text))
        {
            return;
        }

        var result = new TranscriptionResult
        {
            Text = e.Result.Text,
            SpeakerId = e.Result.SpeakerId,
            IsFinal = false,
            Timestamp = DateTime.UtcNow
        };

        _results.Add(result);
        OnTranscribing?.Invoke(this, result);
    }

    private void ConversationTranscriber_Transcribed(object? sender, ConversationTranscriptionEventArgs e)
    {
        if (e.Result.Reason != ResultReason.RecognizedSpeech || string.IsNullOrEmpty(e.Result.Text))
        {
            return;
        }

        var result = new TranscriptionResult
        {
            Text = e.Result.Text,
            SpeakerId = e.Result.SpeakerId,
            IsFinal = true,
            Timestamp = DateTime.UtcNow
        };

        _results.Add(result);
        OnTranscribed?.Invoke(this, result);
    }

    private void ConversationTranscriber_Canceled(object? sender, ConversationTranscriptionCanceledEventArgs e)
    {
        var details = CancellationDetails.FromResult(e.Result);
        var message = $"CANCELED: Reason={details.Reason}; ErrorCode={details.ErrorCode}; Details={details.ErrorDetails}";
        OnError?.Invoke(this, message);
    }

    private void ConversationTranscriber_SessionStopped(object? sender, SessionEventArgs e)
    {
        if (_isStopping || _activeCancellation == null || _activeCancellation.IsCancellationRequested)
        {
            OnStatus?.Invoke(this, "Transcription session stopped.");
            return;
        }

        OnStatus?.Invoke(this, "Transcription paused by service. Attempting to resume...");

        _ = Task.Run(async () =>
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                DisposeTranscriberInstance();

                if (_activeCancellation == null || _activeCancellation.IsCancellationRequested)
                {
                    return;
                }

                await StartTranscriberAsync(_activeCancellation.Token).ConfigureAwait(false);
                OnStatus?.Invoke(this, "Transcription resumed.");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Failed to resume transcription: {ex.Message}");
            }
            finally
            {
                _stateLock.Release();
            }
        });
    }

    private void DisposeTranscriberInstance()
    {
        var transcriber = _transcriber;
        if (transcriber == null)
        {
            return;
        }

        _transcriber = null;

        transcriber.Transcribing -= ConversationTranscriber_Transcribing;
        transcriber.Transcribed -= ConversationTranscriber_Transcribed;
        transcriber.Canceled -= ConversationTranscriber_Canceled;
        transcriber.SessionStarted -= ConversationTranscriber_SessionStarted;
        transcriber.SessionStopped -= ConversationTranscriber_SessionStopped;

        transcriber.Dispose();
    }

    private SpeechConfig CreateSpeechConfig()
    {
        if (!string.IsNullOrWhiteSpace(_subscriptionKey))
        {
            return SpeechConfig.FromEndpoint(new Uri(_conversationEndpoint), _subscriptionKey);
        }

        return SpeechConfig.FromEndpoint(new Uri(_conversationEndpoint), _credential);
    }

    private static void ConfigureSpeechConfig(SpeechConfig config)
    {
        config.SpeechRecognitionLanguage = "en-US";
        config.EnableDictation();
        config.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
        config.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "30000");
        config.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "30000");
    }

    private static string BuildDefaultEndpoint(string region) =>
        $"https://{region}.stt.speech.microsoft.com";

    private static string BuildConversationEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        try
        {
            var builder = new UriBuilder(endpoint);
            var path = builder.Path.Trim('/');

            if (string.IsNullOrEmpty(path) ||
                !path.Contains("speech/recognition/conversation", StringComparison.OrdinalIgnoreCase))
            {
                builder.Path = "speech/recognition/conversation/cognitiveservices/v1";
            }

            return builder.Uri.ToString().TrimEnd('/');
        }
        catch
        {
            return $"{endpoint.TrimEnd('/')}/speech/recognition/conversation/cognitiveservices/v1";
        }
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        try
        {
            var builder = new UriBuilder(endpoint) { Path = string.Empty };
            return builder.Uri.ToString().TrimEnd('/');
        }
        catch
        {
            return endpoint.TrimEnd('/');
        }
    }

    private static string? TryExtractRegionFromEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        try
        {
            var uri = new Uri(endpoint);
            var parts = uri.Host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : null;
        }
        catch
        {
            return null;
        }
    }
}

public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public string SpeakerId { get; set; } = "Unknown";
    public bool IsFinal { get; set; }
    public DateTime Timestamp { get; set; }
}
