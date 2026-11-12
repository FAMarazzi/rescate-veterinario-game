/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 6:55:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 6:55:00 PM
 * @Description: Controla efectos dinámicos de iluminación en la clínica: parpadeo de tubos fluorescentes dañados
 *   (usando Perlin Noise) y pulso/rotación de luces de emergencia en el Quirófano y pasillos.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;

[RequireComponent(typeof(Light))]
public class ClinicLightEffects : MonoBehaviour
{
    public enum LightEffectMode
    {
        FluorescentFlicker, // Tubo fluorescente con falso contacto
        EmergencyPulse,     // Alarma roja pulsante (Quirófano / Emergencia)
        SubtleBreathing     // Variación suave ambiental
    }

    [SerializeField] private LightEffectMode _mode = LightEffectMode.FluorescentFlicker;
    [SerializeField] private float _baseIntensity = 3.5f;
    [SerializeField] private float _minIntensity = 0.4f;
    [SerializeField] private float _speed = 3.0f;
    [SerializeField] private bool _rotateLight = false;
    [SerializeField] private float _rotationSpeed = 90f;

    private Light _light;
    private float _noiseOffset;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _noiseOffset = Random.Range(0f, 100f);
    }

    public void Configure(LightEffectMode mode, float baseIntensity, float minIntensity, float speed, bool rotate = false)
    {
        _mode = mode;
        _baseIntensity = baseIntensity;
        _minIntensity = minIntensity;
        _speed = speed;
        _rotateLight = rotate;
    }

    private void Update()
    {
        if (_light == null) return;

        switch (_mode)
        {
            case LightEffectMode.FluorescentFlicker:
                // Uso PerlinNoise para simular caídas bruscas de tensión en un tubo fluorescente dañado
                float noise = Mathf.PerlinNoise((Time.time * _speed) + _noiseOffset, _noiseOffset);
                _light.intensity = noise > 0.28f ? _baseIntensity : _minIntensity;
                break;

            case LightEffectMode.EmergencyPulse:
                // Onda senoidal suave entre intensidad mínima y máxima para sirena de emergencia
                float pulse = (Mathf.Sin(Time.time * _speed) + 1f) * 0.5f;
                _light.intensity = Mathf.Lerp(_minIntensity, _baseIntensity, pulse);
                break;

            case LightEffectMode.SubtleBreathing:
                float breath = (Mathf.Sin(Time.time * _speed + _noiseOffset) + 1f) * 0.5f;
                _light.intensity = Mathf.Lerp(_minIntensity, _baseIntensity, breath);
                break;
        }

        if (_rotateLight)
        {
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}

