/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 7:10:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/19/2026, 1:48:06 PM
 * @Description: Gestor de audio general (Singleton): música de fondo, efectos de sonido (disparo, curación,
 *   jaula abierta, golpe) y control de volumen maestro persistente con PlayerPrefs.
 *   Si no se asignan AudioClips externos en el Inspector, sintetiza los clips proceduralmente para que nunca falle.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string VolumePrefKey = "MasterVolume";

    [Header("Clips Opcionales (Si están vacíos se cargan de Resources/Audio o se generan por código)")]
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] private AudioClip _dartShootClip;
    [SerializeField] private AudioClip _meleeClip;
    [SerializeField] private AudioClip _cageOpenClip;
    [SerializeField] private AudioClip _healClip;
    [SerializeField] private AudioClip _catCuredClip;
    [SerializeField] private AudioClip _uiHoverClip;
    [SerializeField] private AudioClip _uiClickClip;

    [Header("Volumen de Música (Menú vs Partida)")]
    [Tooltip("Volumen de la música en el Menú Principal, Créditos e Intro")]
    [SerializeField] [Range(0f, 1f)] private float _menuMusicVolume = 0.46f;
    [Tooltip("Volumen un poquito más bajo durante la partida para que resalten los efectos de sonido")]
    [SerializeField] [Range(0f, 1f)] private float _gameplayMusicVolume = 0.25f;
    [SerializeField] private float _volumeDuckSpeed = 2.5f;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private float _masterVolume = 0.8f;

    public float MasterVolume => _masterVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            go.AddComponent<AudioManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _masterVolume = PlayerPrefs.GetFloat(VolumePrefKey, 0.8f);
        AudioListener.volume = _masterVolume;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = _menuMusicVolume;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.volume = 0.75f;

        EnsureClips();
        PlayMusic();
    }

    private void Update()
    {
        if (_bgmSource == null) return;

        // En el Menú Principal / Intro / Créditos suena a _menuMusicVolume (0.55), y al iniciar la partida baja suave a _gameplayMusicVolume (0.30)
        bool inMenu = GameManager.IsMenuOpen;
        float targetVol = inMenu ? _menuMusicVolume : _gameplayMusicVolume;
        if (!Mathf.Approximately(_bgmSource.volume, targetVol))
        {
            _bgmSource.volume = Mathf.MoveTowards(_bgmSource.volume, targetVol, _volumeDuckSpeed * Time.unscaledDeltaTime);
        }

        if (_bgmClip != null && !_bgmSource.isPlaying)
        {
            _bgmSource.clip = _bgmClip;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = _masterVolume;
        PlayerPrefs.SetFloat(VolumePrefKey, _masterVolume);
        PlayerPrefs.Save();
    }

    public void PlayMusic()
    {
        if (_bgmSource != null && _bgmClip != null && !_bgmSource.isPlaying)
        {
            _bgmSource.clip = _bgmClip;
            _bgmSource.Play();
        }
    }

    public void PlayDartShoot() => PlaySFX(_dartShootClip, 0.65f);
    public void PlayMelee() => PlaySFX(_meleeClip, 0.7f);
    public void PlayCageOpen() => PlaySFX(_cageOpenClip, 0.85f);
    public void PlayHeal() => PlaySFX(_healClip, 0.85f);
    public void PlayCatCured() => PlaySFX(_catCuredClip, 0.75f);
    public void PlayUIHover() => PlaySFX(_uiHoverClip, 0.42f);
    public void PlayUIClick() => PlaySFX(_uiClickClip, 0.56f);

    private void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (_sfxSource != null && clip != null)
        {
            _sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    // Genera ondas de sonido limpias en memoria si no se arrastraron archivos .wav/.mp3
    private void EnsureClips()
    {
        if (_dartShootClip == null)
            _dartShootClip = CreateToneClip("DartShoot", 680f, 240f, 0.11f);
        if (_meleeClip == null)
            _meleeClip = CreateToneClip("MeleePush", 190f, 85f, 0.14f);
        if (_cageOpenClip == null)
            _cageOpenClip = CreateArpeggioClip("CageRescue", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.36f);
        if (_healClip == null)
            _healClip = CreateArpeggioClip("MedkitHeal", new float[] { 440f, 523.25f, 659.25f }, 0.28f); // Tríada de La menor (A4 - C5 - E5)
        if (_catCuredClip == null)
            _catCuredClip = CreateArpeggioClip("CatCured", new float[] { 587.33f, 880f }, 0.22f);
        if (_uiHoverClip == null)
            _uiHoverClip = CreateSoftPluckClip("UIHover_Am", new float[] { 659.25f }, 0.045f); // Mi5 (5ta de La menor), cortito y sutil
        if (_uiClickClip == null)
            _uiClickClip = CreateSoftPluckClip("UIClick_Am", new float[] { 440f, 659.25f }, 0.095f); // La4 -> Mi5 (tónica a 5ta de La menor)

        if (_bgmClip == null)
            _bgmClip = Resources.Load<AudioClip>("Audio/Rescate Veterinario master simple");
#if UNITY_EDITOR
        if (_bgmClip == null)
            _bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/Rescate Veterinario master simple.wav");
#endif
        if (_bgmClip == null)
            _bgmClip = CreateAmbientLoopClip("ClinicAmbientBGM", 8.0f);
    }

    private AudioClip CreateToneClip(string clipName, float startFreq, float endFreq, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += (2f * Mathf.PI * freq) / sampleRate;
            float envelope = 1f - t;
            samples[i] = Mathf.Sin(phase) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateArpeggioClip(string clipName, float[] notes, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            int noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t * notes.Length));
            float freq = notes[noteIndex];
            phase += (2f * Mathf.PI * freq) / sampleRate;
            float envelope = Mathf.Sin(t * Mathf.PI);
            samples[i] = Mathf.Sin(phase) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateAmbientLoopClip(string clipName, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Acorde menor atmosférico suave para la clínica
        float[] chord = { 220f, 261.63f, 329.63f, 392f };
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float note = chord[Mathf.FloorToInt((time * 0.5f) % chord.Length)];
            float lfo = 0.6f + 0.4f * Mathf.Sin(time * Mathf.PI * 0.5f);
            float val = Mathf.Sin(2f * Mathf.PI * note * time) * 0.12f
                      + Mathf.Sin(2f * Mathf.PI * 110f * time) * 0.10f;
            samples[i] = val * lfo;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Genera un toque de UI suave tipo marimba/madera con decaimiento exponencial rápido, afinado en La menor
    private AudioClip CreateSoftPluckClip(string clipName, float[] notes, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            int noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t * notes.Length));
            float freq = notes[noteIndex];
            phase += (2f * Mathf.PI * freq) / sampleRate;

            // Ataque suave de 3ms para no hacer "pop" y caída exponencial rápida para que no cargue el oído
            float attack = Mathf.Clamp01(i / (sampleRate * 0.003f));
            float localT = (t * notes.Length) - noteIndex;
            float decay = Mathf.Exp(-localT * 4.5f);
            samples[i] = Mathf.Sin(phase) * attack * decay * 0.42f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

