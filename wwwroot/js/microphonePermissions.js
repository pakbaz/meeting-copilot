// Simple Microphone Permission Management
// Provides basic functions to check and request microphone access

window.MicrophonePermissions = {
    /**
     * Simple microphone permission check
     */
    async checkMicrophonePermission() {
        try {
            console.log('🎤 Checking microphone permission...');
            
            // Check basic browser support
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                return {
                    hasPermission: false,
                    error: "This browser does not support microphone access. Please use Chrome, Edge, Firefox, or Safari."
                };
            }

            // Check HTTPS requirement
            if (location.protocol !== 'https:' && location.hostname !== 'localhost' && location.hostname !== '127.0.0.1') {
                return {
                    hasPermission: false,
                    error: "Microphone access requires HTTPS. Please access this site securely."
                };
            }

            // Try to access microphone briefly to test permissions
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                stream.getTracks().forEach(track => track.stop());
                console.log('✅ Microphone access granted');
                return { hasPermission: true };
            } catch (error) {
                console.log('❌ Microphone access denied:', error.name);
                
                let errorMessage = "Microphone access denied. ";
                switch (error.name) {
                    case 'NotAllowedError':
                        errorMessage += "Please click 'Allow' when prompted for microphone access.";
                        break;
                    case 'NotFoundError':
                        errorMessage += "No microphone found. Please ensure a microphone is connected.";
                        break;
                    case 'NotReadableError':
                        errorMessage += "Microphone is being used by another application.";
                        break;
                    default:
                        errorMessage += "Please check your browser settings and try again.";
                }
                
                return {
                    hasPermission: false,
                    error: errorMessage
                };
            }
        } catch (error) {
            console.error('🎤 Unexpected error:', error);
            return {
                hasPermission: false,
                error: "Unable to check microphone permissions: " + error.message
            };
        }
    },

    /**
     * Request microphone permission
     */
    async requestMicrophonePermission() {
        try {
            console.log('🎤 Requesting microphone permission...');
            
            const stream = await navigator.mediaDevices.getUserMedia({ 
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true
                }
            });
            
            // Stop immediately after getting permission
            stream.getTracks().forEach(track => track.stop());
            
            console.log('✅ Microphone permission granted');
            return { granted: true };
        } catch (error) {
            console.log('❌ Microphone permission request failed:', error.name);
            
            let errorMessage = "Permission request failed. ";
            if (error.name === 'NotAllowedError') {
                errorMessage += "Please click 'Allow' in the permission dialog, or click the microphone icon in your browser's address bar.";
            } else {
                errorMessage += error.message;
            }
            
            return { granted: false, error: errorMessage };
        }
    },

    /**
     * Get browser-specific instructions
     */
    getMicrophoneInstructions() {
        const userAgent = navigator.userAgent;
        let browser = 'your browser';
        
        if (userAgent.includes('Chrome')) browser = 'Chrome';
        else if (userAgent.includes('Firefox')) browser = 'Firefox';
        else if (userAgent.includes('Safari')) browser = 'Safari';
        else if (userAgent.includes('Edge')) browser = 'Edge';
        
        return `For ${browser}:\n\n` +
               "1. Look for the microphone icon in your address bar\n" +
               "2. Click it and select 'Allow'\n" +
               "3. If no icon appears, check your browser's privacy settings\n" +
               "4. Make sure you're using HTTPS (not HTTP)";
    }
};

console.log('🎤 Simple microphone permissions loaded');