/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 6:55:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/13/2026, 9:22:00 AM
 * @Description: Controla efectos dinámicos de iluminación en la clínica (parpadeo Perlin Noise, alarma pulsante)
 *   y activa sombras suaves (LightShadows.Soft) automáticamente en todas las luces de pasillos y consultorios
 *   cercanos al jugador, desactivándolas en salas lejanas para cuidar el Shadow Atlas y los FPS.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;
using UnityEngine.Rendering;

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
    [SerializeField] private float _cullDistance = 17.5f;   // Si el jugador está a más de 17.5m, apago la luz para ahorrar GPU
    [SerializeField] private float _shadowDistance = 13.0f; // Activo sombras reales en las luces del pasillo/sala donde está el jugador

    private Light _light;
    private Transform _targetToTrack;
    private float _noiseOffset;
    private float _cullCheckTimer;
    private bool _isVisibleRange = true;
    private bool _canCastRoomShadows = true;

    private static bool _sceneRenderersChecked = false;

    private void Awake()
    {
        _light = GetComponent<Light>();
        if (_light != null)
        {
            // Todas las luces de pasillos y consultorios proyectan sombra cuando el jugador está cerca
            // (solo excluimos luces secundarias decorativas muy chicas de alcance < 9.5m para no saturar el atlas)
            _canCastRoomShadows = !gameObject.name.Contains("Secundaria") && _light.range >= 9.5f;
            if (_canCastRoomShadows)
            {
                _light.shadowStrength = 0.9f;
                _light.shadowBias = 0.02f;
                _light.shadowNormalBias = 0.2f;
            }
        }
        _noiseOffset = Random.Range(0f, 100f);
        _cullCheckTimer = 0f; // Chequea en el primer frame
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            _targetToTrack = pc.transform;
        else if (Camera.main != null)
            _targetToTrack = Camera.main.transform;

        if (!_sceneRenderersChecked)
        {
            _sceneRenderersChecked = true;
            EnsureCharactersAndFloorsHaveShadows();
        }
    }

    private void OnDestroy()
    {
        _sceneRenderersChecked = false;
    }

    private static void EnsureCharactersAndFloorsHaveShadows()
    {
        // Aseguramos que la veterinaria, los gatos corruptos y los aliados proyecten sombra siempre
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < entities.Length; i++)
        {
            Renderer[] rends = entities[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rends.Length; r++)
            {
                if (rends[r] is LineRenderer || rends[r].gameObject.name.StartsWith("HB_")) continue;
                rends[r].shadowCastingMode = ShadowCastingMode.On;
                rends[r].receiveShadows = true;
            }
        }

        // Aseguramos que todas las baldosas del piso reciban sombras
        GameObject floorRoot = GameObject.Find("Clinica_Piso");
        if (floorRoot != null)
        {
            Renderer[] floorRends = floorRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < floorRends.Length; i++)
            {
                floorRends[i].receiveShadows = true;
            }
        }
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

        // 1. OPTIMIZACIÓN (Distance Culling cada 0.15s): si la habitación está lejos de la cámara/jugador, apago la luz
        _cullCheckTimer -= Time.deltaTime;
        if (_cullCheckTimer <= 0f)
        {
            _cullCheckTimer = 0.15f;
            if (_targetToTrack == null && Camera.main != null)
                _targetToTrack = Camera.main.transform;

            if (_targetToTrack != null)
            {
                float sqrDist = (transform.position - _targetToTrack.position).sqrMagnitude;
                _isVisibleRange = sqrDist <= (_cullDistance * _cullDistance);
                if (_light.enabled != _isVisibleRange)
                    _light.enabled = _isVisibleRange;

                // Activa sombras suaves en todas las luces del pasillo o consultorio donde está parado el jugador
                if (_isVisibleRange && _canCastRoomShadows)
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
