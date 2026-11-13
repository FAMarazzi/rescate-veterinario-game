/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/12/2026, 4:55:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/12/2026, 4:55:00 PM
 * @Description: Efecto visual de Hover y Presionado para los botones del Menú Principal y Pausa.
 *   - Normal: escala 100%, sprite original beige.
 *   - Hover: escala ~110% suave, transformación de color a turquesa/menta brillante (#5FFFE0) manteniendo ícono y texto negros.
 *   - Presionado: escala ~105%, turquesa más oscuro (#2ED1B2).
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Escalas de Estado")]
    [SerializeField] private float _normalScale = 1.0f;
    [SerializeField] private float _hoverScale = 1.10f;
    [SerializeField] private float _pressedScale = 1.05f;
    [SerializeField] private float _scaleSpeed = 14f;

    [Header("Colores Fallback (si el botón no usa textura propia)")]
    [SerializeField] private Color _normalFallbackColor = new Color(0.88f, 0.80f, 0.77f, 1f);
    [SerializeField] private Color _hoverFallbackColor = new Color(0.38f, 1.00f, 0.88f, 1f);
    [SerializeField] private Color _pressedFallbackColor = new Color(0.20f, 0.83f, 0.71f, 1f);

    private Image _image;
    private Sprite _normalSprite;
    private Sprite _hoverSprite;
    private Sprite _pressedSprite;

    private Vector3 _baseScale = Vector3.one;
    private float _targetScaleMultiplier = 1.0f;
    private bool _isHovered;
    private bool _isPressed;

    // Cache estático para no regenerar texturas si se reinicia la escena
    private static readonly Dictionary<Texture2D, Sprite> HoverSpriteCache = new Dictionary<Texture2D, Sprite>();
    private static readonly Dictionary<Texture2D, Sprite> PressedSpriteCache = new Dictionary<Texture2D, Sprite>();

    private void Awake()
    {
        _image = GetComponent<Image>();
        _baseScale = transform.localScale;
        if (_baseScale.sqrMagnitude < 0.001f)
            _baseScale = Vector3.one;

        InitializeSprites();
    }

    public void SetBaseSprite(Sprite sprite)
    {
        if (_image == null) _image = GetComponent<Image>();
        _image.sprite = sprite;
        InitializeSprites();
        ApplyVisualState();
    }

    private void InitializeSprites()
    {
        if (_image == null || _image.sprite == null) return;

        _normalSprite = _image.sprite;
        Texture2D srcTex = _normalSprite.texture;
        if (srcTex == null || !srcTex.isReadable) return;

        if (!HoverSpriteCache.TryGetValue(srcTex, out _hoverSprite) || _hoverSprite == null)
        {
            // Transforma el papel beige en menta/turquesa brillante (#60FFE0) conservando texto/ícono negro y relieves
            _hoverSprite = CreateTintedButtonSprite(_normalSprite, new Color(0.38f, 1.00f, 0.88f), "_HoverMint");
            HoverSpriteCache[srcTex] = _hoverSprite;
        }

        if (!PressedSpriteCache.TryGetValue(srcTex, out _pressedSprite) || _pressedSprite == null)
        {
            // Transforma el papel beige en turquesa más intenso/oscuro (#2ED4B6) para el estado presionado
            _pressedSprite = CreateTintedButtonSprite(_normalSprite, new Color(0.18f, 0.83f, 0.71f), "_PressedTeal");
            PressedSpriteCache[srcTex] = _pressedSprite;
        }
    }

    private static Sprite CreateTintedButtonSprite(Sprite baseSprite, Color targetBaseColor, string suffix)
    {
        Texture2D src = baseSprite.texture;
        int w = src.width;
        int h = src.height;

        Color32[] pixels = src.GetPixels32();
        Color32[] outPixels = new Color32[pixels.Length];

        // Calculamos la luminancia promedio de los píxeles claros del botón para conservar el relieve 3D exacto
        double sumLum = 0;
        int countLum = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            Color32 p = pixels[i];
            if (p.a > 200 && p.r > 120 && p.g > 100 && p.b > 90)
            {
                sumLum += (0.299 * p.r + 0.587 * p.g + 0.114 * p.b);
                countLum++;
            }
        }
        float avgPaperLum = countLum > 0 ? (float)(sumLum / countLum) : 210f;
        if (avgPaperLum < 120f) avgPaperLum = 210f;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            if (c.a == 0)
            {
                outPixels[i] = c;
                continue;
            }

            // Los píxeles oscuros (ícono negro, letras negras y sombra exterior) mantienen su oscuridad original
            int maxChannel = c.r > c.g ? (c.r > c.b ? c.r : c.b) : (c.g > c.b ? c.g : c.b);
            if (maxChannel < 45)
            {
                outPixels[i] = c;
                continue;
            }

            float pixelLum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            float shadeFactor = pixelLum / avgPaperLum;

            // Transición suave entre el borde antialiasing del texto negro y el cuerpo del botón
            float t = Mathf.Clamp01((maxChannel - 45f) / 80f);

            float targetR = Mathf.Clamp(targetBaseColor.r * 255f * shadeFactor, 0f, 255f);
            float targetG = Mathf.Clamp(targetBaseColor.g * 255f * shadeFactor, 0f, 255f);
            float targetB = Mathf.Clamp(targetBaseColor.b * 255f * shadeFactor, 0f, 255f);

            // En zonas de brillo alto (cintas adhesivas y bisel superior) sumamos un leve toque de luz blanca
            if (shadeFactor > 1.02f)
            {
                float highlight = (shadeFactor - 1.02f) * 85f;
                targetR = Mathf.Clamp(targetR + highlight, 0f, 255f);
                targetG = Mathf.Clamp(targetG + highlight, 0f, 255f);
                targetB = Mathf.Clamp(targetB + highlight, 0f, 255f);
            }

            byte nr = (byte)Mathf.Lerp(c.r, targetR, t);
            byte ng = (byte)Mathf.Lerp(c.g, targetG, t);
            byte nb = (byte)Mathf.Lerp(c.b, targetB, t);

            outPixels[i] = new Color32(nr, ng, nb, c.a);
        }

        Texture2D newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        newTex.name = src.name + suffix;
        newTex.wrapMode = TextureWrapMode.Clamp;
        newTex.filterMode = FilterMode.Bilinear;
        newTex.SetPixels32(outPixels);
        newTex.Apply(false, false);

        return Sprite.Create(
            newTex,
            baseSprite.rect,
            new Vector2(0.5f, 0.5f),
            baseSprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );
    }

    private void OnEnable()
    {
        _isHovered = false;
        _isPressed = false;
        _targetScaleMultiplier = _normalScale;
        transform.localScale = _baseScale * _normalScale;
        ApplyVisualState();
    }

    private void OnDisable()
    {
        _isHovered = false;
        _isPressed = false;
        _targetScaleMultiplier = _normalScale;
        transform.localScale = _baseScale * _normalScale;
        ApplyVisualState();
    }

    private void Update()
    {
        Vector3 desired = _baseScale * _targetScaleMultiplier;
        transform.localScale = Vector3.Lerp(transform.localScale, desired, Time.unscaledDeltaTime * _scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        UpdateState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
        UpdateState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _isPressed = true;
        UpdateState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _isPressed = false;
        UpdateState();
    }

    private void UpdateState()
    {
        if (_isPressed)
            _targetScaleMultiplier = _pressedScale;
        else if (_isHovered)
            _targetScaleMultiplier = _hoverScale;
        else
            _targetScaleMultiplier = _normalScale;

        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (_image == null) return;

        if (_normalSprite != null && _hoverSprite != null && _pressedSprite != null)
        {
            _image.color = Color.white;
            if (_isPressed)
                _image.sprite = _pressedSprite;
            else if (_isHovered)
                _image.sprite = _hoverSprite;
            else
                _image.sprite = _normalSprite;
        }
        else
        {
            if (_isPressed)
                _image.color = _pressedFallbackColor;
            else if (_isHovered)
                _image.color = _hoverFallbackColor;
            else
                _image.color = _normalFallbackColor;
        }
    }
}

