/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 6:55:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/12/2026, 8:05:00 AM
 * @Description: Controla efectos dinámicos de iluminación en la clínica (parpadeo Perlin Noise, alarma pulsante)
 *   y aplica optimización por Distance Culling: apaga automáticamente las luces de las habitaciones lejanas
 *   que están fuera del campo visual de la cámara para mantener altos los FPS.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;

[RequireComponent(typeof(Light))]
public class ClinicLightEffects : MonoBehaviour
{
    public enum LightEffectMode
    {
        StaticCulled,       // Luz fija que solo se apaga/prende por distancia para optimizar GPU
        FluorescentFlicker, // Tubo fluorescente con falso contacto (Perlin Noise)
        EmergencyPulse,     // Alarma roja pulsante (Quirófano / Emergencia)
        SubtleBreathing     // Variación suave ambiental
    }

    [SerializeField] private LightEffectMode _mode = LightEffectMode.StaticCulled;
    [SerializeField] private float _baseIntensity = 12f;
    [SerializeField] private float _minIntensity = 0.5f;
    [SerializeField] private float _speed = 3.0f;
    [SerializeField] private float _cullDistance = 16.5f; // Si el jugador está a más de 16.5m, apago la luz para ahorrar GPU
    [SerializeField] private float _shadowDistance = 11.5f; // Solo activo sombras en la luz de la sala donde está parado el jugador

    private Light _light;
    private Transform _targetToTrack;
    private float _noiseOffset;
    private float _cullCheckTimer;
    private bool _isVisibleRange = true;
    private bool _hadShadowsInitially = false;

    private void Awake()
    {
        _light = GetComponent<Light>();
        if (_light != null)
            _hadShadowsInitially = _light.shadows != LightShadows.None;
        _noiseOffset = Random.Range(0f, 100f);
        _cullCheckTimer = Random.Range(0f, 0.2f); // Desfaso el chequeo entre luces
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            _targetToTrack = pc.transform;
        else if (Camera.main != null)
            _targetToTrack = Camera.main.transform;
    }

    public void Configure(LightEffectMode mode, float baseIntensity, float minIntensity = 0f, float speed = 1f, bool rotate = false)
    {
        _mode = mode;
        _baseIntensity = baseIntensity;
        _minIntensity = minIntensity;
        _speed = speed;
    }

    private void Update()
    {
        if (_light == null) return;

        // 1. OPTIMIZACIÓN (Distance Culling cada 0.2s): si la habitación está lejos de la cámara/jugador, apago la luz
        _cullCheckTimer -= Time.deltaTime;
        if (_cullCheckTimer <= 0f)
        {
            _cullCheckTimer = 0.2f;
            if (_targetToTrack == null && Camera.main != null)
                _targetToTrack = Camera.main.transform;

            if (_targetToTrack != null)
            {
                float sqrDist = (transform.position - _targetToTrack.position).sqrMagnitude;
                _isVisibleRange = sqrDist <= (_cullDistance * _cullDistance);
                if (_light.enabled != _isVisibleRange)
                    _light.enabled = _isVisibleRange;

                // Solo proyecto sombras de cubemap en la habitación donde está parado el jugador (evita saturar el Shadow Atlas 2048x2048)
                if (_isVisibleRange && _hadShadowsInitially)
                {
                    bool closeForShadows = sqrDist <= (_shadowDistance * _shadowDistance);
                    LightShadows desired = closeForShadows ? LightShadows.Soft : LightShadows.None;
                    if (_light.shadows != desired)
                        _light.shadows = desired;
                }
            }
        }

        if (!_isVisibleRange || _mode == LightEffectMode.StaticCulled) return;

        // 2. Efectos dinámicos solo para las luces que están dentro de la pantalla actual
        switch (_mode)
        {
            case LightEffectMode.FluorescentFlicker:
                float noise = Mathf.PerlinNoise((Time.time * _speed) + _noiseOffset, _noiseOffset);
                _light.intensity = noise > 0.28f ? _baseIntensity : _minIntensity;
                break;

            case LightEffectMode.EmergencyPulse:
                float pulse = (Mathf.Sin(Time.time * _speed) + 1f) * 0.5f;
                _light.intensity = Mathf.Lerp(_minIntensity, _baseIntensity, pulse);
                break;

            case LightEffectMode.SubtleBreathing:
                float breath = (Mathf.Sin(Time.time * _speed + _noiseOffset) + 1f) * 0.5f;
                _light.intensity = Mathf.Lerp(_minIntensity, _baseIntensity, breath);
                break;
        }
    }
}
