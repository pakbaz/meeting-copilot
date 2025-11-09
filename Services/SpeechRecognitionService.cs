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
    private readonly string? _subscriptionKey;
    private ConversationTranscriber? _conversationTranscriber;
    private readonly ConcurrentBag<TranscriptionResult> _results = new();
    private TaskCompletionSource<bool>? _recognitionComplete;
    private CancellationToken _activeCancellationToken;
    private Connection? _currentConnection;
    private Task? _credentialWarmupTask;
    private readonly object _warmupLock = new();
    private readonly TokenRequestContext _speechTokenContext = new(new[] { "https://cognitiveservices.azure.com/.default" });

    private enum AuthMethod
    {
        None,
        ManagedIdentity,
        SubscriptionKey
    }

    private AuthMethod _currentAuthMethod = AuthMethod.None;
    private bool _authFallbackAttempted = false;

    public event EventHandler<TranscriptionResult>? OnTranscribing;
    public event EventHandler<TranscriptionResult>? OnTranscribed;
    public event EventHandler<string>? OnError;

    public SpeechRecognitionService(IConfiguration configuration)
    {
        _endpoint = configuration["AzureSpeech:Endpoint"] ??
                Environment.GetEnvironmentVariable("ENDPOINT") ??
                "https://realtime-mssp-resource.cognitiveservices.azure.com/";
        
        // Try multiple secure sources for subscription key (following Azure best practices):
        // 1. User Secrets (for development) - most secure for local dev
        // 2. Environment Variables (for CI/CD scenarios)
        // 3. Azure Key Vault (automatically handled by configuration provider)
        _subscriptionKey = configuration["AzureSpeech:SubscriptionKey"] ?? 
                  configuration["AzureSpeech:SpeechKey"] ??
                  Environment.GetEnvironmentVariable("SPEECH_KEY") ??
                  configuration["AZURE_SPEECH_KEY"] ?? 
                  Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
        
        // Diagnostic logging
        var hasUserSecrets = !string.IsNullOrEmpty(configuration["AzureSpeech:SubscriptionKey"]);
        var hasEnvVar = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SPEECH_KEY")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY"));
        var hasConfigKey = !string.IsNullOrEmpty(_subscriptionKey);
        
        Console.WriteLine($"🔍 Authentication Diagnostics:");
        Console.WriteLine($"   Endpoint: {_endpoint}");
        Console.WriteLine($"   User Secrets available: {hasUserSecrets}");
        Console.WriteLine($"   Environment variable available: {hasEnvVar}");
        Console.WriteLine($"   Subscription key resolved: {hasConfigKey}");
        Console.WriteLine($"   Key length: {(_subscriptionKey?.Length ?? 0)} characters");
        
        // Use DefaultAzureCredential for automatic authentication:
        // - In production: Uses Managed Identity (recommended)
        // - In development: Uses Azure CLI, Visual Studio, or VS Code credentials
        _credential = new DefaultAzureCredential();

        // Kick off credential warm-up in the background so the first recognition is faster.
        _ = WarmUpAsync();
    }

    /// <summary>
    /// Recognizes speech from microphone input with real-time diarization.
    /// Tries Managed Identity first, falls back to subscription key if authentication fails.
    /// </summary>
    public async Task RecognizeFromMicrophoneAsync(CancellationToken cancellationToken, bool forceSubscriptionKey = false)
    {
        SpeechConfig? speechConfig = null;
        string authMethod = "Unknown";
        _activeCancellationToken = cancellationToken;
        if (!forceSubscriptionKey)
        {
            _authFallbackAttempted = false;
        }
        _currentAuthMethod = AuthMethod.None;
        
        try
        {
            // Pre-flight check: Validate microphone access requirements
            await ValidateMicrophoneAccessAsync();

            if (!forceSubscriptionKey)
            {
                await WarmUpAsync();
            }
            
            // Step 1: Try using TokenCredential (Managed Identity/Azure CLI)
            if (!forceSubscriptionKey)
            {
                try
                {
                    OnError?.Invoke(this, "🔐 Step 1: Attempting Managed Identity/Azure CLI authentication...");
                    speechConfig = SpeechConfig.FromEndpoint(new Uri(_endpoint), _credential);
                    authMethod = "Managed Identity/Azure CLI";
                    _currentAuthMethod = AuthMethod.ManagedIdentity;
                    OnError?.Invoke(this, "✅ Managed Identity authentication configured successfully!");
                }
                catch (Exception authEx)
                {
                    OnError?.Invoke(this, $"❌ Managed Identity failed: {authEx.Message}");
                    
                    // Step 2: Fallback to subscription key
                    if (!string.IsNullOrEmpty(_subscriptionKey))
                    {
                        OnError?.Invoke(this, "🔑 Attempting subscription key authentication...");
                        try
                        {
                            speechConfig = SpeechConfig.FromEndpoint(new Uri(_endpoint), _subscriptionKey);
                            authMethod = "Subscription Key";
                            _currentAuthMethod = AuthMethod.SubscriptionKey;
                            OnError?.Invoke(this, "✅ Subscription key authentication configured successfully!");
                        }
                        catch (Exception keyEx)
                        {
                            OnError?.Invoke(this, $"❌ Subscription key failed: {keyEx.Message}");
                            throw new InvalidOperationException($"Both authentication methods failed. Managed Identity: {authEx.Message}. Subscription Key: {keyEx.Message}");
                        }
                    }
                    else
                    {
                        OnError?.Invoke(this, "❌ No subscription key available for fallback!");
                        throw new InvalidOperationException($"Managed Identity authentication failed and no subscription key provided: {authEx.Message}");
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_subscriptionKey))
                {
                    throw new InvalidOperationException("Subscription key not available for fallback authentication.");
                }

                OnError?.Invoke(this, "🔑 Retrying with subscription key authentication...");
                speechConfig = SpeechConfig.FromEndpoint(new Uri(_endpoint), _subscriptionKey);
                authMethod = "Subscription Key";
                _currentAuthMethod = AuthMethod.SubscriptionKey;
                OnError?.Invoke(this, "✅ Subscription key authentication configured successfully!");
            }

            OnError?.Invoke(this, $"🔧 Configuring Speech SDK with {authMethod}...");
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");
            
            // Tune silence timeouts to reduce initial delay while still avoiding premature cutoff
            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "2000");
            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "2000");

            OnError?.Invoke(this, "🎤 Setting up microphone input...");
            
            try
            {
                var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                _conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig);

                try
                {
                    _currentConnection?.Dispose();
                    _currentConnection = Connection.FromRecognizer(_conversationTranscriber);
                    _currentConnection.Open(true); // Pre-open the websocket to shave off connection latency.
                }
                catch (Exception connectionEx)
                {
                    Console.WriteLine($"Connection warm-up failed: {connectionEx.Message}");
                }
            }
            catch (Exception audioEx)
            {
                string microphoneError = GetMicrophoneErrorMessage(audioEx);
                OnError?.Invoke(this, microphoneError);
                throw new InvalidOperationException(microphoneError, audioEx);
            }

            _recognitionComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Subscribe to transcription events
            OnError?.Invoke(this, "📡 Subscribing to transcription events...");
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
            OnError?.Invoke(this, "🚀 Starting speech recognition...");
            
            try
            {
                await _conversationTranscriber.StartTranscribingAsync();
                OnError?.Invoke(this, $"✅ Speech recognition started successfully using {authMethod}! Please speak into your microphone.");
            }
            catch (Exception startEx)
            {
                string startError = GetMicrophoneStartErrorMessage(startEx);
                OnError?.Invoke(this, startError);
                throw new InvalidOperationException(startError, startEx);
            }
        }
        catch (Exception ex)
        {
            string errorMessage = GetDetailedErrorMessage(ex);
            OnError?.Invoke(this, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
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
            _conversationTranscriber.Dispose();
            _conversationTranscriber = null;
        }

        try
        {
            _currentConnection?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection close warning: {ex.Message}");
        }

        _currentConnection?.Dispose();
        _currentConnection = null;
        _recognitionComplete?.TrySetResult(true);
        _currentAuthMethod = AuthMethod.None;
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

                if (_currentAuthMethod == AuthMethod.ManagedIdentity && !_authFallbackAttempted && !string.IsNullOrEmpty(_subscriptionKey))
                {
                    _authFallbackAttempted = true;
                    OnError?.Invoke(this, "ℹ️ Managed Identity authentication was rejected. Retrying with subscription key credentials...");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await RestartWithSubscriptionKeyAsync();
                        }
                        catch (Exception retryEx)
                        {
                            OnError?.Invoke(this, $"❌ Subscription key fallback failed: {retryEx.Message}");
                        }
                    });
                    return;
                }
            }
        }

        OnError?.Invoke(this, errorMessage);
        _recognitionComplete?.TrySetResult(true);
    }

    private void ConversationTranscriber_SessionStopped(object? sender, SessionEventArgs e)
    {
        _recognitionComplete?.TrySetResult(true);
    }

    /// <summary>
    /// Validates microphone access requirements before starting recognition
    /// </summary>
    private async Task ValidateMicrophoneAccessAsync()
    {
        // This is a placeholder for server-side validation
        // The actual microphone permission checking will be done on the client-side
        await Task.CompletedTask;
        
        OnError?.Invoke(this, "🔍 Checking microphone access requirements...");
        OnError?.Invoke(this, "ℹ️ Note: Microphone access requires HTTPS and browser permissions.");
        OnError?.Invoke(this, "ℹ️ Please ensure you've granted microphone access in your browser.");
    }
    
    /// <summary>
    /// Gets a user-friendly error message for microphone-related errors
    /// </summary>
    private string GetMicrophoneErrorMessage(Exception ex)
    {
        string baseMessage = "🎤 Microphone Error: ";
        
        if (ex.Message.Contains("microphone") || ex.Message.Contains("audio") || ex.Message.Contains("device"))
        {
            return baseMessage + "Unable to access microphone. Please ensure:\n" +
                   "• Microphone permissions are granted in your browser\n" +
                   "• You're accessing the site over HTTPS\n" +
                   "• Your microphone is not being used by another application\n" +
                   "• Your browser supports microphone access";
        }
        
        if (ex.Message.Contains("permission") || ex.Message.Contains("denied"))
        {
            return baseMessage + "Permission denied. Please click the microphone icon in your browser's address bar to allow access.";
        }
        
        if (ex.Message.Contains("not found") || ex.Message.Contains("no device"))
        {
            return baseMessage + "No microphone device found. Please ensure a microphone is connected and try again.";
        }
        
        return baseMessage + $"Setup failed: {ex.Message}";
    }
    
    /// <summary>
    /// Gets a user-friendly error message for speech recognition start errors
    /// </summary>
    private string GetMicrophoneStartErrorMessage(Exception ex)
    {
        string baseMessage = "🚀 Speech Recognition Error: ";
        
        if (ex.Message.Contains("microphone") || ex.Message.Contains("audio"))
        {
            return baseMessage + "Failed to start microphone capture. Please refresh the page and ensure microphone access is allowed.";
        }
        
        if (ex.Message.Contains("timeout") || ex.Message.Contains("connection"))
        {
            return baseMessage + "Connection timeout. Please check your internet connection and try again.";
        }
        
        return baseMessage + $"Failed to start: {ex.Message}";
    }
    
    /// <summary>
    /// Gets a detailed error message with troubleshooting information
    /// </summary>
    private string GetDetailedErrorMessage(Exception ex)
    {
        string baseMessage = "💥 Critical Error: ";
        
        // Check for common microphone-related issues
        if (ex.Message.Contains("microphone") || ex.Message.Contains("audio") || 
            ex.Message.Contains("device") || ex.Message.Contains("permission"))
        {
            return baseMessage + "Microphone access failed. Please ensure:\n" +
                   "• You're using a supported browser (Chrome, Edge, Firefox)\n" +
                   "• The site is accessed over HTTPS (required for microphone)\n" +
                   "• Microphone permissions are granted\n" +
                   "• Your microphone is working and not used by other apps";
        }
        
        // Check for authentication issues
        if (ex.Message.Contains("authentication") || ex.Message.Contains("401") || ex.Message.Contains("403"))
        {
            return baseMessage + "Authentication failed. Please check your Azure Speech Service configuration and permissions.";
        }
        
        // Check for network issues
        if (ex.Message.Contains("network") || ex.Message.Contains("connection") || ex.Message.Contains("timeout"))
        {
            return baseMessage + "Network error. Please check your internet connection and Azure Speech Service availability.";
        }
        
        return baseMessage + ex.Message;
    }

    private async Task RestartWithSubscriptionKeyAsync()
    {
        try
        {
            await StopRecognitionAsync();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"ℹ️ Issue encountered while stopping previous session: {ex.Message}");
        }

        if (_activeCancellationToken.IsCancellationRequested)
        {
            OnError?.Invoke(this, "ℹ️ Recognition was cancelled before fallback could be attempted.");
            return;
        }

        await RecognizeFromMicrophoneAsync(_activeCancellationToken, forceSubscriptionKey: true);
    }

    public void Dispose()
    {
        _conversationTranscriber?.Dispose();
        _currentConnection?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Warm up managed identity credentials ahead of recognition to reduce the first-call latency.
    /// </summary>
    public Task WarmUpAsync()
    {
        if (!string.IsNullOrEmpty(_subscriptionKey))
        {
            return Task.CompletedTask;
        }

        lock (_warmupLock)
        {
            _credentialWarmupTask ??= WarmUpCredentialsInternalAsync();
            return _credentialWarmupTask;
        }
    }

    private async Task WarmUpCredentialsInternalAsync()
    {
        try
        {
            await _credential.GetTokenAsync(_speechTokenContext, CancellationToken.None);
            Console.WriteLine("Speech credential warm-up complete.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Speech credential warm-up skipped: {ex.Message}");
        }
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
