/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 7:08:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 7:12:00 PM
 * @Description: Controlador general de la partida (Singleton), Pantalla de Título (Jugar, Opciones con volumen,
 *   Créditos, Salir) y HUD en pantalla. Se suscribe mediante delegados/eventos (Action) al PlayerController,
 *   a todos los Enemy y a las Cage para actualizar barras de vida, contadores, notificaciones y Victoria/Derrota.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private static bool _hasShownTitleThisSession = false;

    private PlayerController _player;
    private int _totalEnemies;
    private int _curedEnemies;
    private int _totalCages;
    private int _openedCages;
    private bool _bossCured;
    private bool _gameEnded;

    // Referencias del HUD generadas automáticamente
    private Image _playerHealthFill;
    private Text _playerHealthText;
    private Text _livesText;
    private Text _medkitsText;
    private Text _enemiesMissionText;
    private Text _cagesMissionText;
    private Text _bossMissionText;
    private Text _notificationText;
    private CanvasGroup _notificationGroup;

    // Pantalla de Fin de Partida (Victoria / Derrota)
    private GameObject _endScreenPanel;
    private Text _endTitleText;
    private Text _endSubtitleText;
    private Image _endPanelBg;

    // Pantalla de Título / Menú Principal (Jugar, Opciones con Slider de Sonido, Créditos, Salir)
    private GameObject _titleMenuPanel;
    private GameObject _optionsSubPanel;
    private GameObject _creditsSubPanel;
    private Text _volumeLabelText;

    private Coroutine _notifCoroutine;

    // Se asegura de que el GameManager exista automáticamente al darle Play en la escena del nivel
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null && FindFirstObjectByType<PlayerController>() != null)
        {
            GameObject go = new GameObject("GameManager_HUD");
            go.AddComponent<GameManager>();
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
        Time.timeScale = 1f;
    }

    private void Start()
    {
        BuildHUD();
        BindSceneEntities();
        UpdateAllHUD();

        // Al arrancar el juego por primera vez mostramos la Pantalla de Título (Jugar, Opciones, Créditos, Salir)
        if (!_hasShownTitleThisSession)
        {
            _hasShownTitleThisSession = true;
            OpenTitleMenu();
        }
        else
        {
            ShowNotification("¡Rescatá la clínica! Sedá a los gatos corruptos y abrí todas las jaulas", new Color(0.75f, 0.95f, 1f));
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Tecla ESC: abre/cierra la Pantalla de Título y Opciones de Sonido
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !_gameEnded)
        {
            if (_titleMenuPanel != null && _titleMenuPanel.activeSelf)
                StartOrResumeGame();
            else
                OpenTitleMenu();
        }

        // Tecla R para reiniciar el nivel rápidamente
        if (Keyboard.current.rKey.wasPressedThisFrame && (_titleMenuPanel == null || !_titleMenuPanel.activeSelf))
        {
            RestartLevel();
        }
    }

    private void BindSceneEntities()
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null)
        {
            _player.OnHealthChanged += HandlePlayerHealthChanged;
            _player.OnLivesChanged += HandlePlayerLivesChanged;
            _player.OnMedkitsChanged += HandlePlayerMedkitsChanged;
            _player.OnOutOfLives += HandlePlayerOutOfLives;
        }

        // Suscripción a todos los enemigos de la clínica
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        _totalEnemies = enemies.Length;
        _curedEnemies = 0;
        _bossCured = _totalEnemies == 0;

        foreach (Enemy enemy in enemies)
        {
            enemy.OnDied += HandleEnemyCured;
        }

        // Conteo y suscripción a todas las jaulas de gatitos
        Cage[] cages = FindObjectsByType<Cage>(FindObjectsSortMode.None);
        _totalCages = cages.Length;
        _openedCages = 0;
        foreach (Cage c in cages)
        {
            if (c.IsOpen) _openedCages++;
        }

        Cage.OnCageOpened += HandleCageOpened;
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnHealthChanged -= HandlePlayerHealthChanged;
            _player.OnLivesChanged -= HandlePlayerLivesChanged;
            _player.OnMedkitsChanged -= HandlePlayerMedkitsChanged;
            _player.OnOutOfLives -= HandlePlayerOutOfLives;
        }

        Cage.OnCageOpened -= HandleCageOpened;
    }

    // =========================================================
    // CALLBACKS DE EVENTOS (PATRÓN OBSERVER)
    // =========================================================
    private void HandlePlayerHealthChanged(float current, float max)
    {
        if (_playerHealthFill != null && max > 0f)
        {
            float ratio = Mathf.Clamp01(current / max);
            _playerHealthFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            _playerHealthFill.color = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(0.2f, 0.9f, 0.45f), ratio);
        }
        if (_playerHealthText != null)
        {
            _playerHealthText.text = $"SALUD: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void HandlePlayerLivesChanged(int currentLives, int maxLives)
    {
        if (_livesText != null)
        {
            string hearts = new string('♥', Mathf.Max(0, currentLives));
            _livesText.text = $"VIDAS: {hearts} ({currentLives}/{maxLives})";
        }

        if (currentLives > 0 && currentLives < maxLives)
        {
            ShowNotification($"¡Cuidado! Perdiste una vida. Te quedan {currentLives} vidas.", new Color(1f, 0.55f, 0.2f));
        }
    }

    private void HandlePlayerMedkitsChanged(int medkits)
    {
        if (_medkitsText != null)
        {
            _medkitsText.text = $"[+] BOTIQUINES: {medkits}  (Usar con Q)";
        }
    }

    private void HandleEnemyCured(Entity entity)
    {
        if (_gameEnded) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCatCured();

        _curedEnemies++;
        bool isBoss = entity != null && (entity.gameObject.name.Contains("Jefe") || entity.MaxHealth >= 150f);
        if (isBoss)
        {
            _bossCured = true;
            ShowNotification("¡SEDASTE AL GATO JEFE DEL QUIRÓFANO!", new Color(1f, 0.88f, 0.25f));
        }
        else
        {
            ShowNotification($"¡Gato corrupto sedado! ({_curedEnemies}/{_totalEnemies})", new Color(0.55f, 1f, 0.75f));
        }

        UpdateMissionPanel();
        CheckVictoryCondition();
    }

    private void HandleCageOpened(Cage cage)
    {
        if (_gameEnded) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCageOpen();

        _openedCages++;
        ShowNotification($"¡Jaula abierta y gatito rescatado! ({_openedCages}/{_totalCages})", new Color(1f, 0.9f, 0.4f));

        UpdateMissionPanel();
        CheckVictoryCondition();
    }

    private void HandlePlayerOutOfLives()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        ShowEndScreen(
            false,
            "¡MISIÓN FALLIDA!",
            $"Te quedaste sin vidas.\nGatos sedados: {_curedEnemies}/{_totalEnemies}  |  Jaulas abiertas: {_openedCages}/{_totalCages}"
        );
    }

    private void CheckVictoryCondition()
    {
        if (_gameEnded) return;

        if (_curedEnemies >= _totalEnemies && _openedCages >= _totalCages)
        {
            _gameEnded = true;
            StartCoroutine(VictoryDelayRoutine());
        }
    }

    private IEnumerator VictoryDelayRoutine()
    {
        ShowNotification("¡TODOS LOS GATITOS ESTÁN A SALVO! ¡CLÍNICA RESCATADA!", new Color(0.3f, 1f, 0.5f));
        yield return new WaitForSeconds(1.2f);

        ShowEndScreen(
            true,
            "¡CLÍNICA RESCATADA!",
            $"¡Excelente trabajo, Veterinaria!\nSedaste a los {_totalEnemies} gatos corruptos (incluido el Jefe) y liberaste las {_totalCages} jaulas."
        );
    }

    private void UpdateAllHUD()
    {
        if (_player != null)
        {
            HandlePlayerHealthChanged(_player.CurrentHealth, _player.MaxHealth);
            HandlePlayerLivesChanged(_player.CurrentLives, _player.MaxLives);
            HandlePlayerMedkitsChanged(_player.Medkits);
        }
        UpdateMissionPanel();
    }

    private void UpdateMissionPanel()
    {
        if (_enemiesMissionText != null)
        {
            bool done = _curedEnemies >= _totalEnemies;
            _enemiesMissionText.text = $"{(done ? "✔" : "•")} Gatos Corruptos Sedados: {_curedEnemies} / {_totalEnemies}";
            _enemiesMissionText.color = done ? new Color(0.4f, 1f, 0.55f) : Color.white;
        }

        if (_cagesMissionText != null)
        {
            bool done = _openedCages >= _totalCages;
            _cagesMissionText.text = $"{(done ? "✔" : "•")} Jaulas de Gatitos Abiertas: {_openedCages} / {_totalCages}";
            _cagesMissionText.color = done ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.92f, 0.6f);
        }

        if (_bossMissionText != null)
        {
            _bossMissionText.text = _bossCured
                ? "✔ Gato Jefe (Quirófano): ¡Sedado!"
                : "• Gato Jefe (Quirófano): Pendiente";
            _bossMissionText.color = _bossCured ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.55f, 0.65f);
        }
    }

    public void ShowNotification(string message, Color color)
    {
        if (_notificationText == null || _notificationGroup == null) return;

        if (_notifCoroutine != null)
            StopCoroutine(_notifCoroutine);

        _notifCoroutine = StartCoroutine(NotificationRoutine(message, color));
    }

    private IEnumerator NotificationRoutine(string message, Color color)
    {
        _notificationText.text = message;
        _notificationText.color = color;
        _notificationGroup.alpha = 1f;

        yield return new WaitForSeconds(3.0f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            _notificationGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.6f);
            yield return null;
        }
        _notificationGroup.alpha = 0f;
    }

    private void ShowEndScreen(bool victory, string title, string subtitle)
    {
        if (_endScreenPanel == null) return;

        _endScreenPanel.SetActive(true);
        _endTitleText.text = title;
        _endTitleText.color = victory ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.35f, 0.35f);
        _endSubtitleText.text = subtitle;
        _endPanelBg.color = victory ? new Color(0.06f, 0.16f, 0.12f, 0.92f) : new Color(0.18f, 0.06f, 0.08f, 0.92f);
    }

    // =========================================================
    // PANTALLA DE TÍTULO, OPCIONES (SONIDO), CRÉDITOS Y SALIR
    // =========================================================
    public void OpenTitleMenu()
    {
        if (_titleMenuPanel == null) return;
        if (_endScreenPanel != null) _endScreenPanel.SetActive(false);

        _titleMenuPanel.SetActive(true);
        _optionsSubPanel.SetActive(false);
        _creditsSubPanel.SetActive(false);
        Time.timeScale = 0f;
    }

    public void StartOrResumeGame()
    {
        if (_titleMenuPanel != null)
            _titleMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        ShowNotification("¡Rescatá la clínica! Sedá a los gatos corruptos y abrí todas las jaulas", new Color(0.75f, 0.95f, 1f));
    }

    private void ToggleOptionsPanel()
    {
        if (_optionsSubPanel == null) return;
        bool nextState = !_optionsSubPanel.activeSelf;
        _optionsSubPanel.SetActive(nextState);
        if (_creditsSubPanel != null) _creditsSubPanel.SetActive(false);
    }

    private void ToggleCreditsPanel()
    {
        if (_creditsSubPanel == null) return;
        bool nextState = !_creditsSubPanel.activeSelf;
        _creditsSubPanel.SetActive(nextState);
        if (_optionsSubPanel != null) _optionsSubPanel.SetActive(false);
    }

    private void OnVolumeSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
        else
            AudioListener.volume = value;

        if (_volumeLabelText != null)
            _volumeLabelText.text = $"Volumen General: {Mathf.RoundToInt(value * 100f)}%";
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =========================================================
    // CONSTRUCCIÓN PROCEDURAL DEL HUD Y PANTALLA DE TÍTULO
    // =========================================================
    private void BuildHUD()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        GameObject canvasObj = new GameObject("HUD_Canvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
            defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 22);

        // 1. PANEL SUPERIOR IZQUIERDO: VIDA, VIDAS Y BOTIQUINES
        RectTransform leftPanel = CreatePanel(canvasObj.transform, "Panel_Jugador",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -24f), new Vector2(430f, 165f), new Color(0.07f, 0.10f, 0.15f, 0.84f));

        RectTransform hbBg = CreatePanel(leftPanel, "HealthBar_BG",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -18f), new Vector2(394f, 36f), new Color(0.18f, 0.18f, 0.22f, 1f));

        GameObject fillObj = new GameObject("HealthBar_Fill");
        fillObj.transform.SetParent(hbBg, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        _playerHealthFill = fillObj.AddComponent<Image>();
        _playerHealthFill.color = new Color(0.2f, 0.9f, 0.45f);

        _playerHealthText = CreateText(hbBg, "Health_Text", defaultFont, 20, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);

        _livesText = CreateText(leftPanel, "Lives_Text", defaultFont, 22, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -82f), new Vector2(-20f, 34f), new Color(1f, 0.45f, 0.55f));

        _medkitsText = CreateText(leftPanel, "Medkits_Text", defaultFont, 20, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -124f), new Vector2(-20f, 34f), new Color(0.45f, 1f, 0.72f));

        CreateText(canvasObj.transform, "Controls_Hint", defaultFont, 17, FontStyle.Normal, TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(1150f, 32f),
            new Color(0.85f, 0.92f, 1f, 0.8f)).text =
            "WASD: Mover  |  Click Der + Izq: Dardo Sedante  |  Espacio: Empujón Melee  |  E: Jaula / Botiquín  |  Q: Curarse  |  R: Reiniciar  |  ESC: Menú";

        // 2. PANEL SUPERIOR DERECHO: OBJETIVOS DE LA MISIÓN
        RectTransform rightPanel = CreatePanel(canvasObj.transform, "Panel_Mision",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -24f), new Vector2(460f, 165f), new Color(0.07f, 0.10f, 0.15f, 0.84f));

        CreateText(rightPanel, "Mission_Header", defaultFont, 20, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -22f), new Vector2(-20f, 28f),
            new Color(0.65f, 0.88f, 1f)).text = "OBJETIVOS DE RESCATE";

        _enemiesMissionText = CreateText(rightPanel, "Mission_Enemies", defaultFont, 19, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -62f), new Vector2(-20f, 28f), Color.white);

        _cagesMissionText = CreateText(rightPanel, "Mission_Cages", defaultFont, 19, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -98f), new Vector2(-20f, 28f), Color.white);

        _bossMissionText = CreateText(rightPanel, "Mission_Boss", defaultFont, 19, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -134f), new Vector2(-20f, 28f), Color.white);

        // 3. BANNER CENTRAL DE NOTIFICACIONES
        RectTransform notifPanel = CreatePanel(canvasObj.transform, "Panel_Notificacion",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(780f, 54f), new Color(0.05f, 0.08f, 0.12f, 0.88f));
        _notificationGroup = notifPanel.gameObject.AddComponent<CanvasGroup>();
        _notificationGroup.alpha = 0f;

        _notificationText = CreateText(notifPanel, "Notif_Text", defaultFont, 22, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);

        // 4. PANTALLA MODAL DE VICTORIA / DERROTA
        RectTransform endRect = CreatePanel(canvasObj.transform, "Panel_FinPartida",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 420f), new Color(0.06f, 0.14f, 0.12f, 0.94f));
        _endScreenPanel = endRect.gameObject;
        _endPanelBg = endRect.GetComponent<Image>();

        _endTitleText = CreateText(endRect, "End_Title", defaultFont, 46, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -75f), new Vector2(-40f, 70f), Color.white);

        _endSubtitleText = CreateText(endRect, "End_Subtitle", defaultFont, 24, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -185f), new Vector2(-60f, 110f), new Color(0.9f, 0.95f, 1f));

        CreateButton(endRect, "Btn_Restart", defaultFont, "REINICIAR NIVEL (Tecla R)",
            new Vector2(-155f, -120f), new Vector2(280f, 58f), new Color(0.18f, 0.65f, 0.42f), RestartLevel);

        CreateButton(endRect, "Btn_Menu", defaultFont, "PANTALLA DE TÍTULO",
            new Vector2(155f, -120f), new Vector2(260f, 58f), new Color(0.25f, 0.42f, 0.68f), OpenTitleMenu);

        _endScreenPanel.SetActive(false);

        // 5. PANTALLA DE TÍTULO COMPLETA (JUGAR, OPCIONES CON SLIDER DE VOLUMEN, CRÉDITOS, SALIR)
        BuildTitleMenu(canvasObj.transform, defaultFont);
    }

    private void BuildTitleMenu(Transform canvasTransform, Font font)
    {
        RectTransform bgRect = CreatePanel(canvasTransform, "Panel_PantallaTitulo",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.04f, 0.07f, 0.12f, 0.92f));
        _titleMenuPanel = bgRect.gameObject;

        RectTransform card = CreatePanel(bgRect, "Card_Central",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(820f, 640f), new Color(0.08f, 0.13f, 0.20f, 0.98f));

        CreateText(card, "Title", font, 52, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -65f), new Vector2(-40f, 70f),
            new Color(0.35f, 0.95f, 0.72f)).text = "RESCATE VETERINARIO";

        CreateText(card, "Subtitle", font, 21, FontStyle.Italic, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -115f), new Vector2(-40f, 36f),
            new Color(0.75f, 0.88f, 1f)).text = "Sedá a los gatos corruptos, rescatá a Dibu y Cuti y liberá todas las jaulas";

        // Botones principales requeridos por la cátedra: Jugar, Opciones, Créditos, Salir
        CreateButton(card, "Btn_Play", font, "▶  JUGAR",
            new Vector2(-210f, 95f), new Vector2(280f, 56f), new Color(0.18f, 0.68f, 0.44f), StartOrResumeGame);

        CreateButton(card, "Btn_Options", font, "⚙  OPCIONES (SONIDO)",
            new Vector2(-210f, 20f), new Vector2(280f, 56f), new Color(0.22f, 0.48f, 0.75f), ToggleOptionsPanel);

        CreateButton(card, "Btn_Credits", font, "★  CRÉDITOS",
            new Vector2(-210f, -55f), new Vector2(280f, 56f), new Color(0.45f, 0.35f, 0.72f), ToggleCreditsPanel);

        CreateButton(card, "Btn_RestartMenu", font, "↻  REINICIAR NIVEL",
            new Vector2(-210f, -130f), new Vector2(280f, 56f), new Color(0.65f, 0.48f, 0.20f), RestartLevel);

        CreateButton(card, "Btn_Exit", font, "✖  SALIR",
            new Vector2(-210f, -205f), new Vector2(280f, 56f), new Color(0.72f, 0.22f, 0.26f), QuitGame);

        // Subpanel de Opciones (Slider de Volumen General)
        RectTransform optRect = CreatePanel(card, "SubPanel_Opciones",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(155f, -35f), new Vector2(390f, 320f), new Color(0.05f, 0.08f, 0.13f, 0.95f));
        _optionsSubPanel = optRect.gameObject;

        float currentVol = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : AudioListener.volume;
        _volumeLabelText = CreateText(optRect, "Vol_Label", font, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -65f), new Vector2(-30f, 40f), Color.white);
        _volumeLabelText.text = $"Volumen General: {Mathf.RoundToInt(currentVol * 100f)}%";

        CreateVolumeSlider(optRect, currentVol);

        CreateText(optRect, "Opt_Hint", font, 18, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 70f), new Vector2(-30f, 80f),
            new Color(0.75f, 0.85f, 0.95f)).text = "Mové la barra para regular el volumen general de la música y efectos de sonido.";

        // Subpanel de Créditos
        RectTransform credRect = CreatePanel(card, "SubPanel_Creditos",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(155f, -35f), new Vector2(390f, 320f), new Color(0.05f, 0.08f, 0.13f, 0.95f));
        _creditsSubPanel = credRect.gameObject;

        CreateText(credRect, "Cred_Text", font, 20, FontStyle.Normal, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, -30f), Color.white).text =
            "CRÉDITOS\n\n" +
            "Desarrollo, Programación y Nivel:\nFederico Andrés Marazzi\n\n" +
            "Materia:\nProgramación de Videojuegos II\n\n" +
            "Protagonistas Felinos:\nDibu y Cuti";

        _optionsSubPanel.SetActive(true); // Dejo visible el control de sonido por defecto a la derecha
        _creditsSubPanel.SetActive(false);
        _titleMenuPanel.SetActive(false);
    }

    private void CreateVolumeSlider(Transform parent, float initialValue)
    {
        GameObject sliderObj = new GameObject("Slider_Volumen");
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(310f, 36f);
        sliderRect.anchoredPosition = new Vector2(0f, 15f);

        // Fondo de la barra
        RectTransform bg = CreatePanel(sliderRect, "Background",
            new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.22f, 0.26f, 0.34f, 1f));

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderRect, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-5f, 0f);

        RectTransform fill = CreatePanel(fillAreaRect, "Fill",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.25f, 0.90f, 0.55f, 1f));

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderRect, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        RectTransform handle = CreatePanel(handleAreaRect, "Handle",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(24f, 0f), Color.white);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = panelObj.AddComponent<Image>();
        img.color = bgColor;
        return rt;
    }

    private Text CreateText(Transform parent, string name, Font font, int fontSize, FontStyle style, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    private void CreateButton(Transform parent, string name, Font font, string label, Vector2 anchoredPos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform btnRect = CreatePanel(parent, name,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            anchoredPos, size, color);

        Button btn = btnRect.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnRect.GetComponent<Image>();
        btn.onClick.AddListener(onClick);

        CreateText(btnRect, "Label", font, 20, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white).text = label;
    }
}
