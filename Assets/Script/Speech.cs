using System.Speech.Synthesis;
using UnityEngine;

public static class Speech
{
    private static SpeechSynthesizer synth;
    private static bool loaded = false;

    public static void Init()
    {
        if (loaded) return;
        synth = new SpeechSynthesizer();
        synth.SetOutputToDefaultAudioDevice();
        loaded = true;
    }

    public static void Interrupt(string text)
    {
        if (!loaded) Init();
        synth.SpeakAsyncCancelAll();
        synth.SpeakAsync(text);
    }

    public static void Queue(string text)
    {
        if (!loaded) Init();
        synth.SpeakAsync(text);
    }

    public static void Silence()
    {
        if (!loaded) return;
        synth.SpeakAsyncCancelAll();
    }

    public static void Shutdown()
    {
        if (!loaded) return;
        synth.Dispose();
        loaded = false;
    }
}