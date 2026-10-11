using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Generates procedural placeholder SFX clips at runtime when the SfxLibrary
    /// has no real audio assigned. Uses sine waves, noise, and envelopes to
    /// produce recognizable combat sounds. Registered as IProceduralAudio.
    /// </summary>
    public interface IProceduralAudio
    {
        AudioClip GetClip(SfxCategory category);
    }

    public class ProceduralAudioGenerator : MonoBehaviour, IProceduralAudio
    {
        [SerializeField, Range(0.1f, 0.5f)] private float defaultDuration = 0.15f;

        private void Awake() => ServiceLocator.Register<IProceduralAudio>(this);
        private void OnDestroy() => ServiceLocator.Unregister<IProceduralAudio>(this);

        public AudioClip GetClip(SfxCategory category)
        {
            return category switch
            {
                SfxCategory.LightAttack => GenerateTone(800f, 0.08f, SineSweep(800f, 400f, 0.08f)),
                SfxCategory.HeavyAttack => GenerateTone(200f, 0.18f, NoiseBurst(0.18f)),
                SfxCategory.Hit => GenerateTone(300f, 0.1f, NoiseBurst(0.1f)),
                SfxCategory.Block => GenerateTone(1200f, 0.06f, SineSweep(1200f, 600f, 0.06f)),
                SfxCategory.Parry => GenerateTone(1500f, 0.12f, SineSweep(1500f, 2000f, 0.12f)),
                SfxCategory.Dodge => GenerateTone(400f, 0.12f, SineSweep(400f, 200f, 0.12f)),
                SfxCategory.Death => GenerateTone(150f, 0.4f, NoiseBurst(0.4f)),
                SfxCategory.GuardBreak => GenerateTone(100f, 0.2f, NoiseBurst(0.2f)),
                SfxCategory.MenuConfirm => GenerateTone(600f, 0.05f, SineSweep(600f, 800f, 0.05f)),
                _ => GenerateTone(440f, defaultDuration, SineSweep(440f, 220f, defaultDuration))
            };
        }

        private static AudioClip GenerateTone(float baseFreq, float duration, float[] samples)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var clip = AudioClip.Create("ProcSfx", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float[] SineSweep(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = 1f - t;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.3f;
            }
            return samples;
        }

        private static float[] NoiseBurst(float duration)
        {
            int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[count];
            var rng = new System.Random(42);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float envelope = Mathf.Exp(-t * 6f);
                samples[i] = ((float)rng.NextDouble() * 2f - 1f) * envelope * 0.25f;
            }
            return samples;
        }
    }
}
