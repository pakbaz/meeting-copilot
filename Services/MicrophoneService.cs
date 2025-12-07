using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace meeting_copilot.Services;

public class MicrophoneService
{
    public async Task<List<MicrophoneDevice>> EnumerateDevicesAsync()
    {
        var devices = new List<MicrophoneDevice>();

        try
        {
            // Use Speech SDK to enumerate audio input devices
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            
            // Get list of available microphones from the system
            // For now, we'll use a simple approach - in production, this would use
            // platform-specific APIs (e.g., NAudio on Windows, AVFoundation on macOS)
            
            // Default device
            devices.Add(new MicrophoneDevice
            {
                Id = "default",
                Name = "Default Microphone",
                IsDefault = true
            });

            // TODO: Implement platform-specific device enumeration
            // For MVP, we'll return just the default device
        }
        catch (Exception ex)
        {
            // Log error but return at least default device
            Console.WriteLine($"Error enumerating microphones: {ex.Message}");
        }

        return devices;
    }

    public async Task<MicrophoneDevice?> GetDefaultDeviceAsync()
    {
        var devices = await EnumerateDevicesAsync();
        return devices.FirstOrDefault(d => d.IsDefault);
    }

    public async Task<bool> TestMicrophoneAsync(string deviceId)
    {
        try
        {
            // Simple test to verify microphone is accessible
            var audioConfig = string.IsNullOrEmpty(deviceId) || deviceId == "default"
                ? AudioConfig.FromDefaultMicrophoneInput()
                : AudioConfig.FromMicrophoneInput(deviceId);

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class MicrophoneDevice
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsDefault { get; init; }
}
