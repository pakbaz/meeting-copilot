using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Core;

namespace meeting_copilot.Services;

/// <summary>
/// Service for handling real-time speech-to-text with diarization.
/// Uses Azure Cognitive Services Speech SDK with Managed Identity authentication.
/// Follows Azure best practices by using DefaultAzureCredential instead of API keys.
/// </summary>
public class SpeechRecognitionService : IDisposable
{
    private readonly string _endpoint;
    private readonly TokenCredential _credential;
    private ConversationTranscriber? _conversationTranscriber;
    private readonly ConcurrentBag<TranscriptionResult> _results = new();
    private TaskCompletionSource<bool>? _recognitionComplete;

    public event EventHandler<TranscriptionResult>? OnTranscribing;
    public event EventHandler<TranscriptionResult>? OnTranscribed;
    public event EventHandler<string>? OnError;

    public SpeechRecognitionService(IConfiguration configuration)
    {
        _endpoint = configuration["AzureSpeech:Endpoint"] ?? "https://gpt-realtime-sp.cognitiveservices.azure.com/";
        
        // Use DefaultAzureCredential for automatic authentication:
        // - In production: Uses Managed Identity
        // - In development: Uses Azure CLI, Visual Studio, or VS Code credentials
        _credential = new DefaultAzureCredential();
    }

    /// <summary>
    /// Recognizes speech from microphone input with real-time diarization.
    /// Uses Azure Managed Identity for secure authentication.
    /// </summary>
    public async Task RecognizeFromMicrophoneAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Use TokenCredential for authentication instead of subscription key
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(_endpoint), _credential);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            _conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig);

            _recognitionComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Subscribe to transcription events
            _conversationTranscriber.Transcribing += ConversationTranscriber_Transcribing;
            _conversationTranscriber.Transcribed += ConversationTranscriber_Transcribed;
            _conversationTranscriber.Canceled += ConversationTranscriber_Canceled;
            _conversationTranscriber.SessionStopped += ConversationTranscriber_SessionStopped;

            // Handle cancellation
            cancellationToken.Register(async () =>
            {
                try
                {
                    await StopRecognitionAsync();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, $"Error during cancellation: {ex.Message}");
                }
            });

            // Start transcription - this will continue running in background
            await _conversationTranscriber.StartTranscribingAsync();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Error during recognition: {ex.Message}");
            throw;
        }
    }



    /// <summary>
    /// Stops the current transcription session.
    /// </summary>
    public async Task StopRecognitionAsync()
    {
        if (_conversationTranscriber != null)
        {
            await _conversationTranscriber.StopTranscribingAsync();
        }
        _recognitionComplete?.TrySetResult(true);
    }

    /// <summary>
    /// Gets all transcription results collected so far.
    /// </summary>
    public IEnumerable<TranscriptionResult> GetResults()
    {
        return _results.ToList();
    }

    /// <summary>
    /// Clears all collected results.
    /// </summary>
    public void ClearResults()
    {
        _results.Clear();
    }

    private void ConversationTranscriber_Transcribing(object? sender, ConversationTranscriptionEventArgs e)
    {
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
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
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
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            OnError?.Invoke(this, "Speech could not be recognized.");
        }
    }

    private void ConversationTranscriber_Canceled(object? sender, ConversationTranscriptionCanceledEventArgs e)
    {
        string errorMessage = $"Recognition canceled. Reason: {e.Reason}";

        if (e.Reason == CancellationReason.Error)
        {
            errorMessage += $"\nError Code: {e.ErrorCode}\nDetails: {e.ErrorDetails}";
            
            // Provide more specific error guidance
            if (e.ErrorCode == CancellationErrorCode.ConnectionFailure)
            {
                errorMessage += "\nCheck your internet connection and Speech service configuration.";
            }
            else if (e.ErrorCode == CancellationErrorCode.AuthenticationFailure)
            {
                errorMessage += "\nAuthentication failed. Please ensure you have the proper role assignment on the Speech resource.";
            }
        }

        OnError?.Invoke(this, errorMessage);
        _recognitionComplete?.TrySetResult(true);
    }

    private void ConversationTranscriber_SessionStopped(object? sender, SessionEventArgs e)
    {
        _recognitionComplete?.TrySetResult(true);
    }

    public void Dispose()
    {
        _conversationTranscriber?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a single transcription result with speaker information.
/// </summary>
public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public string SpeakerId { get; set; } = "Unknown";
    public bool IsFinal { get; set; }
    public DateTime Timestamp { get; set; }
}
