/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 7:05:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 7:05:00 PM
 * @Description: Barra de vida flotante (World-Space) para todas las unidades (Enemigos, Jefe y Aliados).
 *   Se suscribe a los eventos OnHealthChanged y OnDied de Entity aplicando el patrón Observer.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    private Entity _entity;
    private Transform _barRoot;
    private Transform _fillTransform;
    private Renderer _fillRenderer;
    private Camera _mainCam;

    private float _barWidth = 0.85f;
    private float _barHeight = 0.11f;
    private float _heightOffset = 1.35f;
    private Color _fullColor = new Color(0.95f, 0.22f, 0.28f);
    private Color _lowColor = new Color(1.0f, 0.65f, 0.1f);

    private static Material _sharedUnlitMat;

    public void AttachTo(Entity entity, bool isAlly, bool isBoss = false)
    {
        _entity = entity;
        _mainCam = Camera.main;

        if (isBoss)
        {
            _barWidth = 1.45f;
            _barHeight = 0.16f;
            _heightOffset = 1.85f;
            _fullColor = new Color(0.95f, 0.15f, 0.45f);
        }
        else if (isAlly)
        {
            _barWidth = 0.75f;
            _barHeight = 0.10f;
            _heightOffset = 1.15f;
            _fullColor = new Color(0.2f, 0.95f, 0.4f);
            _lowColor = new Color(0.95f, 0.85f, 0.2f);
        }

        CreateBarVisuals();

        // Me suscribo a los eventos de Entity (Patrón Observer: solo actualizo cuando cambia la vida)
        _entity.OnHealthChanged += HandleHealthChanged;
        _entity.OnDied += HandleDied;

        HandleHealthChanged(_entity.CurrentHealth, _entity.MaxHealth);
    }

    private void CreateBarVisuals()
    {
        if (_sharedUnlitMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            _sharedUnlitMat = new Material(s);
        }

        _barRoot = new GameObject("HealthBar_Root").transform;
        _barRoot.SetParent(transform, false);

        // Compenso la escala del padre por si es el Gato Jefe (1.85x) o un modelo reescalado
        float parentScale = Mathf.Max(0.01f, transform.lossyScale.y);
        _barRoot.localPosition = new Vector3(0f, _heightOffset / parentScale, 0f);
        _barRoot.localScale = Vector3.one / parentScale;

        // Fondo oscuro de la barra
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "HB_Background";
        Destroy(bg.GetComponent<Collider>());
        bg.transform.SetParent(_barRoot, false);
        bg.transform.localScale = new Vector3(_barWidth + 0.06f, _barHeight + 0.05f, 1f);
        Renderer bgRen = bg.GetComponent<Renderer>();
        Material bgMat = new Material(_sharedUnlitMat);
        SetMatColor(bgMat, new Color(0.08f, 0.08f, 0.12f, 0.9f));
        bgRen.material = bgMat;
        bgRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Relleno de vida
        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fill.name = "HB_Fill";
        Destroy(fill.GetComponent<Collider>());
        fill.transform.SetParent(_barRoot, false);
        fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fill.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);

        _fillTransform = fill.transform;
        _fillRenderer = fill.GetComponent<Renderer>();
        Material fillMat = new Material(_sharedUnlitMat);
        SetMatColor(fillMat, _fullColor);
        _fillRenderer.material = fillMat;
        _fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void SetMatColor(Material mat, Color col)
    {
        if (mat == null) return;
        mat.color = col;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (_fillTransform == null || max <= 0f) return;

        float ratio = Mathf.Clamp01(current / max);
        _fillTransform.localScale = new Vector3(_barWidth * ratio, _barHeight, 1f);
        // Acomodo el pivote a la izquierda para que la barra baje de derecha a izquierda
        _fillTransform.localPosition = new Vector3(-(_barWidth * (1f - ratio)) * 0.5f, 0f, -0.01f);

        if (_fillRenderer != null)
        {
            Color c = Color.Lerp(_lowColor, _fullColor, ratio);
            SetMatColor(_fillRenderer.material, c);
        }
    }

    public void SetAllyCorruptedVisual(bool isCorrupted)
    {
        if (_barRoot != null)
            _barRoot.gameObject.SetActive(true);

        if (isCorrupted)
        {
            _fullColor = new Color(0.85f, 0.22f, 0.95f); // Violeta corrupción
            _lowColor = new Color(1.0f, 0.45f, 0.2f);
        }
        else
        {
            _fullColor = new Color(0.2f, 0.95f, 0.4f);  // Verde aliado
            _lowColor = new Color(0.95f, 0.85f, 0.2f);
        }

        if (_entity != null)
            HandleHealthChanged(_entity.CurrentHealth, _entity.MaxHealth);
    }

    private void HandleDied(Entity entity)
    {
        // Si es un Ally, no apago su barra porque nunca muere: pasa de aliado a corrupto o de corrupto a curado
        if (entity is Ally) return;

        if (_barRoot != null)
            _barRoot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_barRoot == null || !_barRoot.gameObject.activeSelf) return;
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam != null)
        {
            // Efecto Billboard: la barra siempre mira derecho a la cámara Top-View 45°
            _barRoot.rotation = _mainCam.transform.rotation;
        }
    }

    private void OnDestroy()
    {
        if (_entity != null)
        {
            _entity.OnHealthChanged -= HandleHealthChanged;
            _entity.OnDied -= HandleDied;
        }
    }
}

