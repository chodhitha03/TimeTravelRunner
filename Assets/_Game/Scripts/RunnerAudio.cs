using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Procedurally synthesised arcade sound effects & driving synth music.
    /// Includes ascending musical coin pickup combos, punchy impacts, and cyber basslines.
    /// </summary>
    public sealed class RunnerAudio
    {
        private readonly AudioSource source;
        private readonly AudioSource ambience;
        public bool Enabled { get; private set; }
        private readonly AudioClip robotStart, robotTick, robotRelease, jetIgnition, hoverStart;
        private readonly AudioClip jump, slide, hit, confirm, power, shield, portal, nearMiss;
        private readonly AudioClip[] coinChimes = new AudioClip[6];
        private int coinCombo = 0;
        private float lastCoinTime = 0f;

        public RunnerAudio(Transform parent)
        {
            GameObject obj = new GameObject("Generated sound effects");
            obj.transform.SetParent(parent, false);
            source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = .32f;

            // Ascending pentatonic coin pickups: C5, D5, E5, G5, A5, C6
            float[] freqs = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f };
            for (int i = 0; i < coinChimes.Length; i++)
            {
                float baseFreq = freqs[i];
                coinChimes[i] = HarmonicChime("Coin " + i, baseFreq, baseFreq * 1.5f, 0.13f);
            }

            jump = Tone("Jump", 240, 720, .16f, true);
            slide = NoiseSwoosh("Slide", .22f);
            hit = Tone("Hit", 140, 45, .45f, true);
            confirm = HarmonicChime("Menu click", 580, 880, .09f);
            power = HarmonicChime("Power up", 440, 1320, .32f);
            shield = Tone("Shield break", 980, 160, .32f);
            portal = Tone("Time travel portal", 180, 920, .65f);
            nearMiss = HarmonicChime("Near Miss", 880, 1760, .18f);

            robotStart = Tone("Robot autopilot online", 320, 930, .55f);
            robotTick = Tone("Robot handover countdown", 810, 740, .15f);
            robotRelease = Tone("Robot handing back control", 920, 280, .50f);
            jetIgnition = Tone("Jetpack turbo activation", 170, 790, .65f);
            hoverStart = Tone("Hoverboard ion activation", 280, 580, .48f);

            GameObject cityAudio = new GameObject("Cyber City / electronic city ambience");
            cityAudio.transform.SetParent(parent, false);
            ambience = cityAudio.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.loop = true;
            ambience.spatialBlend = 0f;
            ambience.volume = .08f;
            ambience.clip = CyberSynthGroove();

            Enabled = PlayerPrefs.GetInt("TTR_SOUND_ON", 1) != 0;
            source.mute = !Enabled;
            ambience.mute = !Enabled;
        }

        private AudioClip HarmonicChime(string name, float startFreq, float endFreq, float seconds)
        {
            const int rate = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(rate * seconds));
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            float[] wave = new float[samples];
            float phase = 0;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(1f - t, 0.6f));
                // Add rich harmonics for clean bell tone
                float sample = Mathf.Sin(phase) * 0.70f +
                               Mathf.Sin(phase * 2f) * 0.22f +
                               Mathf.Sin(phase * 3f) * 0.08f;
                wave[i] = sample * env * 0.40f;
            }
            clip.SetData(wave, 0);
            return clip;
        }

        private AudioClip Tone(string name, float startFrequency, float endFrequency, float seconds, bool bassPunch = false)
        {
            const int rate = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(rate * seconds));
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            float[] wave = new float[samples];
            float phase = 0;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += 2f * Mathf.PI * frequency / rate;
                float fade = Mathf.Sin(Mathf.PI * (1f - t));
                float sample = Mathf.Sin(phase);
                if (bassPunch) sample += Mathf.Sin(phase * 0.5f) * 0.4f;
                wave[i] = sample * fade * .42f;
            }
            clip.SetData(wave, 0);
            return clip;
        }

        private AudioClip NoiseSwoosh(string name, float seconds)
        {
            const int rate = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(rate * seconds));
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            float[] wave = new float[samples];
            System.Random rand = new System.Random(42);
            float filter = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                filter += (white - filter) * 0.25f;
                float env = Mathf.Sin(Mathf.PI * t);
                wave[i] = filter * env * 0.35f;
            }
            clip.SetData(wave, 0);
            return clip;
        }

        private static AudioClip CyberSynthGroove()
        {
            const int rate = 22050;
            const int count = rate * 8; // 8-second seamless looping synth groove
            AudioClip clip = AudioClip.Create("Cyber City / Synth Groove", count, 1, rate, false);
            float[] samples = new float[count];
            float bpm = 128f;
            float beatDuration = 60f / bpm;

            for (int i = 0; i < count; i++)
            {
                float time = (float)i / rate;
                float beatTime = (time % beatDuration) / beatDuration;
                
                // 4-on-the-floor kick pulse
                float kickEnv = Mathf.Clamp01(1f - beatTime * 4.5f);
                float kickPhase = 2f * Mathf.PI * Mathf.Lerp(120f, 45f, beatTime) * time;
                float kick = Mathf.Sin(kickPhase) * kickEnv * 0.35f;

                // Driving 16th note synth bass arpeggio
                float bassStep = Mathf.Floor((time % (beatDuration * 2f)) / (beatDuration * 0.25f));
                float[] bassNotes = { 55f, 55f, 65.4f, 73.4f, 82.4f, 73.4f, 65.4f, 55f };
                float noteFreq = bassNotes[(int)bassStep % bassNotes.Length];
                float stepTime = (time % (beatDuration * 0.25f)) / (beatDuration * 0.25f);
                float bassEnv = Mathf.Clamp01(1f - stepTime * 2.2f);
                float bass = Mathf.Sin(2f * Mathf.PI * noteFreq * time) * bassEnv * 0.22f;

                // High hat tick on off-beats
                float hatTime = ((time + beatDuration * 0.5f) % beatDuration) / beatDuration;
                float hatEnv = Mathf.Clamp01(1f - hatTime * 12f);
                float hat = (float)(new System.Random(i).NextDouble() * 2.0 - 1.0) * hatEnv * 0.06f;

                samples[i] = kick + bass + hat;
            }
            clip.SetData(samples, 0);
            return clip;
        }

        public void SetSoundEnabled(bool value)
        {
            Enabled = value;
            source.mute = !Enabled;
            ambience.mute = !Enabled;
            PlayerPrefs.SetInt("TTR_SOUND_ON", Enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void CyberAmbience(bool playing)
        {
            if (ambience == null) return;
            if (playing && !ambience.isPlaying) ambience.Play();
            else if (!playing && ambience.isPlaying) ambience.Stop();
        }

        public void RobotStart() { Play(robotStart); }
        public void RobotCountdown() { Play(robotTick); }
        public void RobotRelease() { Play(robotRelease); }
        public void JetStart() { Play(jetIgnition); }
        public void HoverboardStart() { Play(hoverStart); }

        private void Play(AudioClip clip) { if (source != null && Enabled) source.PlayOneShot(clip); }
        
        public void Coin()
        {
            if (Time.time - lastCoinTime < 0.75f)
                coinCombo = (coinCombo + 1) % coinChimes.Length;
            else
                coinCombo = 0;
            lastCoinTime = Time.time;
            Play(coinChimes[coinCombo]);
        }

        public void Jump() { Play(jump); }
        public void Slide() { Play(slide); }
        public void Hit() { Play(hit); }
        public void Confirm() { Play(confirm); }
        public void PowerUp() { Play(power); }
        public void ShieldHit() { Play(shield); }
        public void Portal() { Play(portal); }
        public void NearMiss() { Play(nearMiss); }
    }
}
