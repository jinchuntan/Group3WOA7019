using System;
using System.IO;
using UnityEngine;

public class VoiceNoteManager : MonoBehaviour
{
    public AudioSource audioSource;

    private const int Frequency = 44100;
    private const int MaxRecordSeconds = 30;

    private AudioClip recordedClip;
    private bool isRecording = false;
    private string deviceName;

    public bool IsRecording
    {
        get { return isRecording; }
    }

    public bool HasRecording
    {
        get { return recordedClip != null && recordedClip.samples > 0; }
    }

    private void Awake()
    {
        EnsureAudioSource();
    }

    public bool StartRecording()
    {
        EnsureAudioSource();

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No microphone device found.");
            return false;
        }

        if (isRecording)
        {
            UnityEngine.Debug.Log("Already recording.");
            return true;
        }

        deviceName = Microphone.devices[0];
        recordedClip = Microphone.Start(deviceName, false, MaxRecordSeconds, Frequency);
        isRecording = true;

        UnityEngine.Debug.Log("Recording Started using microphone: " + deviceName);
        return true;
    }

    public bool StopRecording()
    {
        if (!isRecording)
        {
            UnityEngine.Debug.LogWarning("Not recording. Press Record before Stop.");
            return false;
        }

        int recordedSamples = Microphone.GetPosition(deviceName);
        Microphone.End(deviceName);
        isRecording = false;

        if (recordedClip == null || recordedSamples <= 0)
        {
            recordedClip = null;
            UnityEngine.Debug.LogWarning("Recording stopped, but no audio samples were captured. Check microphone permission/input.");
            return false;
        }

        int channels = recordedClip.channels;
        float[] samples = new float[recordedSamples * channels];
        recordedClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "VoiceNoteRecording",
            recordedSamples,
            channels,
            recordedClip.frequency,
            false
        );
        trimmedClip.SetData(samples, 0);
        recordedClip = trimmedClip;

        UnityEngine.Debug.Log("Recording Stopped. Captured " + recordedClip.length.ToString("0.00") + " seconds.");
        return true;
    }

    public bool PlayRecording()
    {
        EnsureAudioSource();

        if (recordedClip == null || recordedClip.samples <= 0)
        {
            UnityEngine.Debug.LogWarning("No voice recording available. Record first, then stop, then play.");
            return false;
        }

        audioSource.Stop();
        audioSource.clip = recordedClip;
        audioSource.volume = 1f;
        audioSource.mute = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        UnityEngine.Debug.Log("Playing Recording. Length: " + recordedClip.length.ToString("0.00") + " seconds.");
        return true;
    }

    public string SaveRecording(string noteId)
    {
        if (string.IsNullOrEmpty(noteId))
        {
            UnityEngine.Debug.LogWarning("Cannot save voice note because noteId is empty.");
            return string.Empty;
        }

        if (!HasRecording)
        {
            UnityEngine.Debug.LogWarning("Cannot save voice note because there is no recording.");
            return string.Empty;
        }

        string dir = Path.Combine(Application.persistentDataPath, "voice_notes");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, noteId + ".wav");
        WriteWav(path, recordedClip);
        UnityEngine.Debug.Log("Voice note saved to: " + path);
        return path;
    }

    public bool LoadRecording(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            recordedClip = null;
            return false;
        }

        recordedClip = ReadWav(path);
        bool ok = HasRecording;
        UnityEngine.Debug.Log(ok ? "Voice note loaded: " + path : "Voice note load failed: " + path);
        return ok;
    }

    public void ClearRecording()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
        recordedClip = null;
        isRecording = false;
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.mute = false;
    }

    private static void WriteWav(string path, AudioClip clip)
    {
        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int sampleCount = clip.samples * channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            int byteRate = sampleRate * channels * 2;
            int dataSize = sampleCount * 2;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
                writer.Write(value);
            }
        }
    }

    private static AudioClip ReadWav(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length < 44)
        {
            return null;
        }

        int channels = BitConverter.ToInt16(bytes, 22);
        int sampleRate = BitConverter.ToInt32(bytes, 24);
        int bitsPerSample = BitConverter.ToInt16(bytes, 34);
        if (bitsPerSample != 16)
        {
            UnityEngine.Debug.LogWarning("Unsupported WAV bit depth: " + bitsPerSample);
            return null;
        }

        int dataIndex = FindDataChunk(bytes);
        if (dataIndex < 0)
        {
            return null;
        }

        int dataSize = BitConverter.ToInt32(bytes, dataIndex + 4);
        int dataStart = dataIndex + 8;
        int sampleCount = dataSize / 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short value = BitConverter.ToInt16(bytes, dataStart + (i * 2));
            samples[i] = value / 32768f;
        }

        AudioClip clip = AudioClip.Create(
            Path.GetFileNameWithoutExtension(path),
            sampleCount / channels,
            channels,
            sampleRate,
            false
        );
        clip.SetData(samples, 0);
        return clip;
    }

    private static int FindDataChunk(byte[] bytes)
    {
        for (int i = 12; i < bytes.Length - 8; i += 2)
        {
            if (bytes[i] == 'd' && bytes[i + 1] == 'a' && bytes[i + 2] == 't' && bytes[i + 3] == 'a')
            {
                return i;
            }
        }
        return -1;
    }
}
