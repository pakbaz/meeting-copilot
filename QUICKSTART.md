# Quick Start Guide - Meeting Copilot

Get up and running with real-time speech-to-text diarization in 5 minutes!

## Prerequisites

- .NET 10.0 or later installed
- Azure Cognitive Services Speech subscription
- Modern browser (Chrome, Edge, Firefox, Safari)

## Quick Setup

### Step 1: Get Azure Speech Credentials

1. Go to [Azure Portal](https://portal.azure.com)
2. Create or navigate to your **Speech resource**
3. Copy the **Endpoint** and **Subscription Key**

### Step 2: Configure the App

Edit `appsettings.json`:

```json
{
  "AzureSpeech": {
    "Endpoint": "https://gpt-realtime-sp.cognitiveservices.azure.com/",
    "SubscriptionKey": "YOUR_KEY_HERE"
  }
}
```

Replace `YOUR_KEY_HERE` with your actual subscription key.

### Step 3: Run the Application

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run the app
dotnet run
```

The app will be available at `https://localhost:7120`

## First Use

1. Open the app in your browser
2. Allow microphone access when prompted
3. Click **"Start Recognition"**
4. Speak clearly into your microphone
5. Watch the transcription appear with speaker identification!
6. Click **"Stop Recognition"** when done

## What You'll See

- **Guest-1, Guest-2, etc.** - Different speakers with color coding
- **Bold text** - Confirmed final transcription
- *Italic text* - Temporary results being processed
- **Statistics** - Number of speakers and total utterances

## Testing

Use multiple people or simulate multiple speakers to see diarization in action!

### Single Speaker Test
```bash
# Start the app and speak several sentences
"Hello, this is my first sentence. And here's my second sentence."
```

### Multi-Speaker Test
Simulate using the included sample audio:
```bash
# Download sample: https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/sampledata/audiofiles/katiesteve.wav
# Update SpeechRecognitionService to use: AudioConfig.FromWavFileInput("katiesteve.wav")
```

## Troubleshooting

**"Microphone not detected"**
- Check browser microphone permissions
- Verify microphone works in system settings
- Try different browser

**"Connection refused"**
- Check Azure endpoint in appsettings.json
- Verify internet connection
- Check Azure Speech resource is active

**"No speech recognized"**
- Speak louder and more clearly
- Reduce background noise
- Ensure microphone is pointing toward you
- Check microphone volume isn't muted

## Key Features

| Feature | Status | Details |
|---------|--------|---------|
| Real-time STT | ✅ | Live transcription as you speak |
| Speaker Diarization | ✅ | Identifies different speakers |
| Intermediate Results | ✅ | Shows text being processed |
| Color Coding | ✅ | Different color per speaker |
| Statistics | ✅ | Tracks speakers and utterances |
| File Upload | 📋 | Coming soon |
| Export | 📋 | Coming soon |

## Next Steps

After getting familiar with the basic functionality:

1. **Deploy to Azure** - Host the app as an Azure App Service
2. **Add File Upload** - Process recorded meetings
3. **Integrate Storage** - Save transcripts to database
4. **Add Analytics** - Extract insights from meetings
5. **Multi-language** - Support additional languages

## Architecture Overview

```
┌─────────────────────────────┐
│   Browser (Blazor UI)        │
│  ┌───────────────────────┐  │
│  │ Home Component        │  │
│  │ - Start/Stop buttons  │  │
│  │ - Live transcription  │  │
│  │ - Speaker stats       │  │
│  └───────────────────────┘  │
└────────────┬────────────────┘
             │ WebSocket (Real-time)
┌────────────▼────────────────┐
│  Blazor Server (ASP.NET)     │
│  ┌───────────────────────┐  │
│  │ SpeechRecognition     │  │
│  │ Service               │  │
│  │ - Microphone capture  │  │
│  │ - Audio processing    │  │
│  │ - Event streaming     │  │
│  └───────────────────────┘  │
└────────────┬────────────────┘
             │ HTTPS/REST
┌────────────▼────────────────┐
│  Azure Speech Service        │
│  - Real-time STT             │
│  - Speaker Diarization       │
│  - Language Detection        │
└──────────────────────────────┘
```

## Performance Tips

- **Bandwidth**: Uses ~64kbps for audio streaming
- **Latency**: ~200-500ms for intermediate results
- **Final Results**: ~500ms-2s depending on speech length
- **Concurrent Users**: Scales with Azure resource SKU

## Support & Learning

- 📚 [Official Quickstart](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization)
- 🔧 [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- 🎓 [Speech SDK Samples](https://github.com/Azure-Samples/cognitive-services-speech-sdk)
- 💬 Check the main README.md for detailed documentation

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "Speech key not set" | Add key to appsettings.json |
| "Endpoint format wrong" | Use full URL: `https://region.cognitiveservices.azure.com/` |
| "Microphone permission denied" | Grant microphone access in browser settings |
| "No output after speaking" | Check network connection and Azure service status |
| "App won't build" | Run `dotnet restore` then `dotnet build` |

## Environment Variables (Advanced)

Instead of appsettings.json, you can use:

```bash
# Windows
setx AZURESPEECH_ENDPOINT "https://your-region.cognitiveservices.azure.com/"
setx AZURESPEECH_SUBSCRIPTIONKEY "your-key"

# Linux/macOS
export AZURESPEECH_ENDPOINT="https://your-region.cognitiveservices.azure.com/"
export AZURESPEECH_SUBSCRIPTIONKEY="your-key"
```

Then update Program.cs to read from environment variables.

---

**Ready to go?** Start with Step 1 above and you'll be transcribing speech in minutes!
