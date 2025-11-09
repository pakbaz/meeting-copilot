# Meeting Copilot - Real-Time Speech-to-Text with Diarization

A Blazor web application that provides real-time speech-to-text transcription with speaker identification (diarization) using Azure AI Services.

## Features

- 🎤 **Real-time Speech Recognition** - Transcribe speech from microphone in real-time
- 👥 **Speaker Identification** - Automatically identify and distinguish between different speakers (Guest-1, Guest-2, etc.)
- 📝 **Live Transcription Display** - See transcription results update in real-time with speaker labels
- 📊 **Statistics** - Track total utterances and unique speakers in the conversation
- 🎨 **Modern UI** - Clean, responsive Bootstrap-based interface
- ⚡ **Interactive Server Rendering** - Blazor InteractiveServer components for seamless updates

## Architecture

### Components

1. **SpeechRecognitionService** (`Services/SpeechRecognitionService.cs`)
   - Manages Azure Cognitive Services Speech SDK integration
   - Handles real-time diarization with speaker identification
   - Emits events for transcribing and transcribed results
   - Supports both microphone and file-based input

2. **Home Component** (`Components/Pages/Home.razor`)
   - Interactive Blazor component with real-time updates
   - Control panel for starting/stopping recognition
   - Live transcription display with color-coded speakers
   - Statistics dashboard showing speaker information

3. **Program.cs**
   - Registers SpeechRecognitionService as a scoped service
   - Configures Razor components with interactive server rendering

## Prerequisites

- .NET 10.0 or later
- Azure Cognitive Services Speech API subscription
- Azure Speech resource endpoint and key
- Modern web browser with microphone access

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd meeting-copilot
```

### 2. Configure Azure Speech Service

1. Create an Azure Speech resource in the Azure portal
2. Get your:
   - **Endpoint**: e.g., `https://gpt-realtime-sp.cognitiveservices.azure.com/`
   - **Subscription Key**: Available in your resource's keys section

3. Update `appsettings.json`:

```json
{
  "AzureSpeech": {
    "Endpoint": "https://YOUR-RESOURCE.cognitiveservices.azure.com/",
    "SubscriptionKey": "YOUR-SUBSCRIPTION-KEY"
  }
}
```

### 3. Restore Dependencies

```bash
dotnet restore
```

This will install:
- `Microsoft.CognitiveServices.Speech` (v1.43.0) - Azure Speech SDK
- All other required NuGet packages

### 4. Build the Project

```bash
dotnet build
```

### 5. Run the Application

```bash
dotnet run
```

The application will be available at `https://localhost:7120` (or the configured port).

## Usage

1. **Allow Microphone Access**: Grant browser permission to access your microphone
2. **Start Recognition**: Click the "Start Recognition" button
3. **Speak**: Begin speaking into your microphone
4. **View Results**: Watch real-time transcription appear with speaker identification
5. **Stop Recognition**: Click "Stop Recognition" when done
6. **Clear Results**: Use "Clear Results" to reset the transcript

## Real-Time Diarization Features

### Speaker Identification
- **Guest-1**: First speaker (Primary - Blue)
- **Guest-2**: Second speaker (Info - Cyan)
- **Guest-3**: Third speaker (Success - Green)
- **Guest-4**: Fourth speaker (Warning - Orange)
- **Guest-5**: Fifth speaker (Danger - Red)
- **Unknown**: Speaker not yet identified

### Intermediate vs Final Results
- **Italic text (gray)**: Intermediate results being processed
- **Bold text (black)**: Final confirmed transcription

## Configuration

### appsettings.json

```json
{
  "AzureSpeech": {
    "Endpoint": "https://gpt-realtime-sp.cognitiveservices.azure.com/",
    "SubscriptionKey": ""
  }
}
```

### Environment Variables (Alternative)

Instead of hardcoding in `appsettings.json`, you can use environment variables:

```bash
setx AZURE_SPEECH_ENDPOINT "https://your-resource.cognitiveservices.azure.com/"
setx AZURE_SPEECH_SUBSCRIPTION_KEY "your-key"
```

## Development

### Project Structure

```
meeting-copilot/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor          # Main speech recognition UI
│   │   ├── Counter.razor       # Counter demo
│   │   └── ...
│   ├── Layout/
│   └── _Imports.razor
├── Services/
│   └── SpeechRecognitionService.cs  # Azure Speech SDK wrapper
├── Properties/
├── wwwroot/                    # Static files
├── appsettings.json           # Configuration
├── Program.cs                 # Service registration
└── meeting-copilot.csproj    # Project file
```

### Key Classes

#### `SpeechRecognitionService`

```csharp
// Start recognition from microphone
await speechService.RecognizeFromMicrophoneAsync(cancellationToken);

// Start recognition from file
await speechService.RecognizeFromFileAsync("audio.wav", cancellationToken);

// Stop recognition
await speechService.StopRecognitionAsync();

// Get results
var results = speechService.GetResults();

// Subscribe to events
speechService.OnTranscribing += (sender, result) => { /* handle intermediate */ };
speechService.OnTranscribed += (sender, result) => { /* handle final */ };
speechService.OnError += (sender, error) => { /* handle error */ };
```

#### `TranscriptionResult`

```csharp
public class TranscriptionResult
{
    public string Text { get; set; }           // Transcribed text
    public string SpeakerId { get; set; }      // Speaker identifier (Guest-1, Guest-2, etc.)
    public bool IsFinal { get; set; }          // True if final result, false if intermediate
    public DateTime Timestamp { get; set; }    // When the result was received
}
```

## API Reference

### Azure Speech Service Settings

The application automatically configures diarization with these settings:

- **Language**: English (en-US) - configurable in `SpeechRecognitionService`
- **Diarization**: Enabled with intermediate results
- **PropertyId.SpeechServiceResponse_DiarizeIntermediateResults**: `"true"`

## Troubleshooting

### "Speech resource key and endpoint values not set"
- Check that `appsettings.json` has correct values
- Verify environment variables if using them
- Ensure the subscription key is valid and not expired

### "Speech could not be recognized"
- Check microphone permissions in browser settings
- Ensure microphone is working properly
- Verify network connectivity to Azure

### "Did you set the speech resource key and endpoint values?"
- Confirm the endpoint URL is in correct format
- Verify the subscription key is from the correct region
- Check that the Speech resource is active in Azure portal

### Audio Quality Issues
- Speak clearly and at a normal pace
- Reduce background noise
- Ensure microphone is positioned correctly
- Test microphone in browser's audio settings

## Security Considerations

1. **Never commit credentials**: Always use `appsettings.Development.json` for local development
2. **Use Key Vault**: For production, store secrets in Azure Key Vault
3. **Environment Variables**: Use for CI/CD pipelines
4. **Managed Identity**: Recommended for Azure-hosted applications

Example for Key Vault integration:

```csharp
var keyVaultUrl = new Uri("https://your-keyvault.vault.azure.net/");
var credential = new DefaultAzureCredential();
var client = new SecretClient(keyVaultUrl, credential);

var speechEndpoint = client.GetSecret("SpeechEndpoint").Value.Value;
var speechKey = client.GetSecret("SpeechKey").Value.Value;
```

## Performance Notes

- **Real-time Processing**: Results are streamed as speaker talks
- **Memory**: Uses concurrent bags to efficiently store results
- **Threading**: All UI updates marshaled through Blazor's dispatcher
- **Resources**: Dispose services properly to release Speech SDK resources

## Limitations

- Maximum 5 speakers tracked with color coding (6th+ get default color)
- Intermediate results are replaced in UI (not accumulated)
- File upload UI not fully implemented (ready for expansion)
- Supports English language only (by default, easily configurable)

## Future Enhancements

- [ ] Multi-language support
- [ ] Audio file upload and processing
- [ ] Export transcription to various formats (JSON, CSV, TXT)
- [ ] Speaker profile matching and identification
- [ ] Sentiment analysis per speaker
- [ ] Meeting highlights extraction
- [ ] Search and filter functionality
- [ ] Playback with transcript sync

## References

- [Azure Cognitive Services Speech SDK Documentation](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/)
- [Speech-to-Text Diarization Quickstart](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Azure AI Services Authentication](https://learn.microsoft.com/en-us/azure/ai-services/authentication)

## License

This project is provided as-is for demonstration purposes.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review Azure Speech Service documentation
3. Check browser console for JavaScript errors
4. Verify Azure resource status and permissions

## Version History

- **v1.0.0** (2025-11-08) - Initial release with real-time diarization
  - Real-time speech-to-text recognition
  - Speaker identification and diarization
  - Interactive Blazor UI with live updates
  - Event-based architecture for extensibility
