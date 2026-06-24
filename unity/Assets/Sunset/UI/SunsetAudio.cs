using UnityEngine;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Процедурное аудио (фаза 10 порта): эмбиент-дрон петлёй, «колокол» на смене
    /// кадра катсцены и «бип» печати «Некого». Звук синтезируется в рантайме
    /// (<see cref="AudioSynth"/>) — никаких звуковых ассетов не требуется, как в
    /// веб-версии (там был WebAudio-граф). Громкость берётся из настроек (музыка).
    ///
    /// Повесь компонент на объект сцены и зови <see cref="StartAmbient"/> / <see cref="Bell"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetAudio : MonoBehaviour
    {
        private AudioSource _ambient;
        private AudioSource _sfx;
        private float _musicVol = 0.7f;

        private void Awake()
        {
            var settings = SettingsSave.Load();
            _musicVol = Mathf.Clamp01(settings.music / 100f);

            int rate = AudioSettings.outputSampleRate;
            if (rate <= 0) rate = 44100;

            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop = true;
            _ambient.playOnAwake = false;
            _ambient.spatialBlend = 0f;
            _ambient.clip = MakeClip("SunsetAmbient", AudioSynth.Ambient(rate), rate);

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false;
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;
        }

        /// <summary>Плавно запускает эмбиент. volScale — доля от громкости музыки (0..1).</summary>
        public void StartAmbient(float volScale = 0.5f)
        {
            if (_ambient == null) return;
            _ambient.volume = 0f;
            if (!_ambient.isPlaying) _ambient.Play();
            StopAllCoroutines();
            StartCoroutine(Fade(_ambient, _musicVol * Mathf.Clamp01(volScale), 2f));
        }

        public void StopAmbient()
        {
            if (_ambient == null || !_ambient.isPlaying) return;
            StopAllCoroutines();
            StartCoroutine(FadeOutStop(_ambient, 1.2f));
        }

        /// <summary>Устанавливает целевую громкость эмбиента (доля от музыки).</summary>
        public void SetAmbientVolume(float volScale)
        {
            if (_ambient == null) return;
            StopAllCoroutines();
            StartCoroutine(Fade(_ambient, _musicVol * Mathf.Clamp01(volScale), 1.5f));
        }

        /// <summary>«Колокол» заданной частоты (на смене кадра катсцены).</summary>
        public void Bell(float freq)
        {
            if (_sfx == null || _musicVol <= 0f) return;
            int rate = AudioSettings.outputSampleRate; if (rate <= 0) rate = 44100;
            var clip = MakeClip("SunsetBell", AudioSynth.Bell(rate, freq), rate);
            _sfx.PlayOneShot(clip, _musicVol);
        }

        /// <summary>«Бип» печати «Некого».</summary>
        public void Tick()
        {
            if (_sfx == null || _musicVol <= 0f) return;
            int rate = AudioSettings.outputSampleRate; if (rate <= 0) rate = 44100;
            var clip = MakeClip("SunsetTick", AudioSynth.Tick(rate), rate);
            _sfx.PlayOneShot(clip, _musicVol * 0.6f);
        }

        private static AudioClip MakeClip(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private System.Collections.IEnumerator Fade(AudioSource src, float to, float dur)
        {
            float from = src.volume, t = 0f;
            while (t < dur) { src.volume = Mathf.Lerp(from, to, t / dur); t += Time.deltaTime; yield return null; }
            src.volume = to;
        }

        private System.Collections.IEnumerator FadeOutStop(AudioSource src, float dur)
        {
            float from = src.volume, t = 0f;
            while (t < dur) { src.volume = Mathf.Lerp(from, 0f, t / dur); t += Time.deltaTime; yield return null; }
            src.volume = 0f;
            src.Stop();
        }
    }
}
