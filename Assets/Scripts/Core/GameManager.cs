/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 7:08:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/20/2026, 8:58:48 AM
 * @Description: Controlador general de la partida (Singleton), Pantalla de Menú de Inicio ilustrada
 *   (con fondo de recepción magenta, logo oficial y botones con efecto Hover 110% turquesa y Presionado 105%),
 *   Menú de Pausa independiente (ESC), Opciones de Sonido, Créditos, Pantallas de Victoria/Derrota y HUD.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Permite que PlayerController sepa si hay algún menú abierto para no disparar ni golpear al hacer clic en botones
    public static bool IsMenuOpen =>
        Instance != null && (
            (Instance._titleMenuPanel != null && Instance._titleMenuPanel.activeSelf) ||
            (Instance._optionsSubPanel != null && Instance._optionsSubPanel.activeSelf) ||
            (Instance._introStoryPanel != null && Instance._introStoryPanel.activeSelf) ||
            (Instance._pauseMenuPanel != null && Instance._pauseMenuPanel.activeSelf) ||
            (Instance._endScreenPanel != null && Instance._endScreenPanel.activeSelf) ||
            (Instance._gameOverScreenPanel != null && Instance._gameOverScreenPanel.activeSelf) ||
            (Instance._victoryScreenPanel != null && Instance._victoryScreenPanel.activeSelf)
        );

    private static bool _hasShownTitleThisSession = false;
    private static bool _sceneHookRegistered = false;

    [Header("Sprites del Menú de Inicio (Assets/MainMenu)")]
    [SerializeField] private Sprite _menuBackgroundSprite;
    [SerializeField] private Sprite _menuLogoSprite;
    [SerializeField] private Sprite _btnJugarSprite;
    [SerializeField] private Sprite _btnOpcionesSprite;
    [SerializeField] private Sprite _btnCreditosSprite;
    [SerializeField] private Sprite _btnSalirSprite;

    [Header("Sprites de Pantalla de Créditos (Assets/Creditos)")]
    [SerializeField] private Sprite _creditsBackgroundSprite;
    [SerializeField] private Sprite _btnVolverSprite;

    [Header("Sprites de Pantalla de Opciones (Assets/Options)")]
    [SerializeField] private Sprite _optionsBackgroundSprite;

    [Header("Sprites de Pantalla de Game Over (Assets/GameOver)")]
    [SerializeField] private Sprite _gameOverBackgroundSprite;
    [SerializeField] private Sprite _btnReintentarGameOverSprite;
    [SerializeField] private Sprite _btnMenuPrincipalGameOverSprite;

    [Header("Sprites de Pantalla de Victoria (Assets/Victory)")]
    [SerializeField] private Sprite _victoryBackgroundSprite;
    [SerializeField] private Sprite _btnJugarDeNuevoVictorySprite;
    [SerializeField] private Sprite _btnMenuPrincipalVictorySprite;

    [Header("Sprite Opcional de Historia / Intro")]
    [SerializeField] private Sprite _introBackgroundSprite;

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
    private GameObject _gameOverScreenPanel;
    private GameObject _victoryScreenPanel;
    private Text _endTitleText;
    private Text _endSubtitleText;
    private Image _endPanelBg;

    // Pantalla de Título / Menú de Inicio Ilustrado
    private GameObject _titleMenuPanel;
    private GameObject _optionsSubPanel;
    private GameObject _creditsSubPanel;
    private Text _volumeLabelText;
    private Text _musicVolumeLabelText;
    private Text _sfxVolumeLabelText;
    private Slider _titleVolumeSlider;
    private Slider _titleMusicVolumeSlider;
    private Slider _titleSfxVolumeSlider;
    private float _lastSfxPreviewTime = -10f;
    private static Sprite _sliderKnobSprite;

    // Pantalla de Historia / Informe de Emergencia (3 diapositivas separadas al darle Jugar)
    private GameObject _introStoryPanel;
    private int _introSlideIndex;
    private Text _introStepText;
    private Text _introSlideTitleText;
    private Text _introSlideBodyText;
    private Image _introAccentStripe;
    private Text _introNextBtnLabel;
    private GameObject _introSkipButtonObj;
    private CanvasGroup _introSlideGroup;
    private Coroutine _introFadeCoroutine;

    // Menú de Pausa Independiente (al apretar ESC durante la partida)
    private GameObject _pauseMenuPanel;
    private Text _pauseVolumeLabelText;
    private Slider _pauseVolumeSlider;

    private Coroutine _notifCoroutine;

    // Se asegura de que el GameManager exista automáticamente al darle Play y también al recargar la escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (!_sceneHookRegistered)
        {
            _sceneHookRegistered = true;
            SceneManager.sceneLoaded += OnSceneLoadedStatic;
        }
        EnsureInstance();
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
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

        LoadMenuSpritesIfNeeded();
    }

    private void Start()
    {
        BuildHUD();
        BindSceneEntities();
        UpdateAllHUD();

        // Al arrancar el juego por primera vez mostramos el Menú de Inicio ilustrado
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

        // Si está abierta la pantalla de Historia / Informe de Emergencia, Enter avanza de diapositiva y ESC vuelve al Menú
        if (_introStoryPanel != null && _introStoryPanel.activeSelf)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                AdvanceIntroSlide();
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                OpenTitleMenu();
            return;
        }

        // Tecla ESC: cierra Opciones/Créditos si están abiertos; si estoy jugando abre o cierra la Pausa
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !_gameEnded)
        {
            if (_optionsSubPanel != null && _optionsSubPanel.activeSelf)
            {
                _optionsSubPanel.SetActive(false);
                return;
            }

            if (_titleMenuPanel != null && _titleMenuPanel.activeSelf)
            {
                if (_creditsSubPanel != null && _creditsSubPanel.activeSelf)
                    _creditsSubPanel.SetActive(false);
                return;
            }

            if (_pauseMenuPanel != null && _pauseMenuPanel.activeSelf)
                ResumeFromPause();
            else
                OpenPauseMenu();
        }

        // Tecla R para reiniciar el nivel rápidamente cuando no hay menús abiertos
        if (Keyboard.current.rKey.wasPressedThisFrame && !IsMenuOpen)
        {
            RestartLevel();
        }
    }

    // Carga los PNGs desde Assets/MainMenu y Assets/Creditos tanto en Editor como desde archivo
    private void LoadMenuSpritesIfNeeded()
    {
        if (_menuBackgroundSprite == null)
            _menuBackgroundSprite = LoadSpriteFromAssetFolder("MainMenu", "Recepción veterinaria bajo luces magenta.png");
        if (_menuLogoSprite == null)
            _menuLogoSprite = LoadSpriteFromAssetFolder("MainMenu", "Logo de Rescate Veterinario.png");
        if (_btnJugarSprite == null)
            _btnJugarSprite = LoadSpriteFromAssetFolder("MainMenu", "boton_jugar.png");
        if (_btnOpcionesSprite == null)
            _btnOpcionesSprite = LoadSpriteFromAssetFolder("MainMenu", "boton_opciones.png");
        if (_btnCreditosSprite == null)
            _btnCreditosSprite = LoadSpriteFromAssetFolder("MainMenu", "boton_creditos.png");
        if (_btnSalirSprite == null)
            _btnSalirSprite = LoadSpriteFromAssetFolder("MainMenu", "boton_salir.png");

        if (_creditsBackgroundSprite == null)
            _creditsBackgroundSprite = LoadSpriteFromAssetFolder("Creditos", "Fondo Créditos del Rescate Veterinario.png");
        if (_btnVolverSprite == null)
            _btnVolverSprite = LoadSpriteFromAssetFolder("Creditos", "Botón de papel rasgado “Volver”.png");

        if (_optionsBackgroundSprite == null)
            _optionsBackgroundSprite = LoadSpriteFromAssetFolder("Options", "Opciones de Rescate Veterinario.png");

        if (_gameOverBackgroundSprite == null)
            _gameOverBackgroundSprite = LoadSpriteFromAssetFolder("GameOver", "Pantalla Game Over de Rescate Veterinario.png");
        if (_btnReintentarGameOverSprite == null)
            _btnReintentarGameOverSprite = LoadSpriteFromAssetFolder("GameOver", "Botón Reintentar con Huellas.png");
        if (_btnMenuPrincipalGameOverSprite == null)
            _btnMenuPrincipalGameOverSprite = LoadSpriteFromAssetFolder("GameOver", "Botón de Menú Principal Neón.png");

        if (_victoryBackgroundSprite == null)
            _victoryBackgroundSprite = LoadSpriteFromAssetFolder("Victory", "¡Clínica Salvada! Nuevos Desafíos.png");
        if (_btnJugarDeNuevoVictorySprite == null)
            _btnJugarDeNuevoVictorySprite = LoadSpriteFromAssetFolder("Victory", "Botón de juego “Jugar de nuevo”.png");
        if (_btnMenuPrincipalVictorySprite == null)
            _btnMenuPrincipalVictorySprite = LoadSpriteFromAssetFolder("Victory", "Botón dorado de menú principal.png");
    }

    private Sprite LoadSpriteFromAssetFolder(string folderName, string fileName)
    {
        string assetPath = "Assets/" + folderName + "/" + fileName;

#if UNITY_EDITOR
        // Primero me fijo si lo puedo cargar directo como Sprite desde AssetDatabase
        Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sp != null && sp.texture != null && sp.texture.isReadable)
            return sp;

        Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex != null && tex.isReadable)
        {
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect
            );
        }
#endif

        // Fallback directo leyendo los bytes del PNG en disco para que siempre sea readable y nítido al 100%
        string fullPath = Path.Combine(Application.dataPath, folderName, fileName);
        if (!File.Exists(fullPath))
            fullPath = Path.Combine(Application.streamingAssetsPath, folderName, fileName);

        if (File.Exists(fullPath))
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (diskTex.LoadImage(bytes, false))
            {
                diskTex.name = Path.GetFileNameWithoutExtension(fileName);
                diskTex.wrapMode = TextureWrapMode.Clamp;
                diskTex.filterMode = FilterMode.Bilinear;
                return Sprite.Create(
                    diskTex,
                    new Rect(0f, 0f, diskTex.width, diskTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect
                );
            }
        }

        return null;
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

        // Suscripción a todos los enemigos de la clínica (incluyendo los que están desactivados en el EnemyPool)
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        if (Instance == this)
            Instance = null;

        if (_player != null)
        {
            _player.OnHealthChanged -= HandlePlayerHealthChanged;
            _player.OnLivesChanged -= HandlePlayerLivesChanged;
            _player.OnMedkitsChanged -= HandlePlayerMedkitsChanged;
            _player.OnOutOfLives -= HandlePlayerOutOfLives;
        }

        Cage.OnCageOpened -= HandleCageOpened;
    }

    // Callbacks de eventos (Patrón Observer)
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
        if (victory && _victoryScreenPanel != null)
        {
            if (_endScreenPanel != null) _endScreenPanel.SetActive(false);
            if (_gameOverScreenPanel != null) _gameOverScreenPanel.SetActive(false);
            _victoryScreenPanel.SetActive(true);
            Time.timeScale = 0f;
            return;
        }

        if (!victory && _gameOverScreenPanel != null)
        {
            if (_endScreenPanel != null) _endScreenPanel.SetActive(false);
            if (_victoryScreenPanel != null) _victoryScreenPanel.SetActive(false);
            _gameOverScreenPanel.SetActive(true);
            Time.timeScale = 0f;
            return;
        }

        if (_endScreenPanel == null) return;

        _endScreenPanel.SetActive(true);
        _endTitleText.text = title;
        _endTitleText.color = victory ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.35f, 0.35f);
        _endSubtitleText.text = subtitle;
        _endPanelBg.color = victory ? new Color(0.06f, 0.16f, 0.12f, 0.94f) : new Color(0.18f, 0.06f, 0.08f, 0.94f);
    }

    // Menú de inicio ilustrado y menú de pausa independiente
    public void OpenTitleMenu()
    {
        if (_titleMenuPanel == null) return;
        if (_endScreenPanel != null) _endScreenPanel.SetActive(false);
        if (_gameOverScreenPanel != null) _gameOverScreenPanel.SetActive(false);
        if (_victoryScreenPanel != null) _victoryScreenPanel.SetActive(false);
        if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
        if (_introStoryPanel != null) _introStoryPanel.SetActive(false);

        _titleMenuPanel.SetActive(true);
        // Arranco con los subpaneles cerrados para que se vea limpio el fondo de recepción magenta
        if (_optionsSubPanel != null) _optionsSubPanel.SetActive(false);
        if (_creditsSubPanel != null) _creditsSubPanel.SetActive(false);
        Time.timeScale = 0f;
    }

    public void OnPlayButtonPressed()
    {
        if (_titleMenuPanel != null)
            _titleMenuPanel.SetActive(false);
        if (_pauseMenuPanel != null)
            _pauseMenuPanel.SetActive(false);

        if (_introStoryPanel != null)
        {
            OpenIntroStory();
        }
        else
        {
            StartOrResumeGame();
        }
    }

    public void OpenIntroStory()
    {
        if (_introStoryPanel == null)
        {
            StartOrResumeGame();
            return;
        }

        _introStoryPanel.SetActive(true);
        Time.timeScale = 0f;
        ShowIntroSlide(0);
    }

    public void AdvanceIntroSlide()
    {
        if (_introSlideIndex < 2)
        {
            ShowIntroSlide(_introSlideIndex + 1);
        }
        else
        {
            StartOrResumeGame();
        }
    }

    private void ShowIntroSlide(int index)
    {
        _introSlideIndex = Mathf.Clamp(index, 0, 2);

        Color[] accents =
        {
            new Color(0.96f, 0.35f, 0.55f),
            new Color(0.38f, 1.00f, 0.88f),
            new Color(1.00f, 0.85f, 0.35f)
        };

        string[] steps =
        {
            "DIAPOSITIVA 1 / 3  •  EXPEDIENTE CLÍNICO #04",
            "DIAPOSITIVA 2 / 3  •  PROTOCOLO DE RESCATE",
            "DIAPOSITIVA 3 / 3  •  ALIADOS Y BOTIQUINES"
        };

        string[] titles =
        {
            "¿QUÉ PASÓ EN LA CLÍNICA?",
            "TU MISIÓN: SIN LASTIMAR A NADIE",
            "TUS COMPAÑEROS: DIBU Y CUTI"
        };

        string[] bodies =
        {
            "¡Alerta en la Clínica Veterinaria!\n\nUn frasco de suero experimental se rompió en el Quirófano y su neblina violeta alteró a los gatos internados, volviéndolos agresivos.\n\nLa clínica quedó tomada y todos los gatitos en sus jaulas esperan que los rescates.",
            "Tomá tu rifle de dardos sedantes:\n\n• Apuntá con [Click Derecho] y dispará con [Click Izquierdo] para dormir a los gatos corruptos (incluido el Gato Jefe del Quirófano).\n• Si se te enciman, empujalos con [Barra Espaciadora].\n• Abrí todas las jaulas con la tecla [E] para liberar a los gatitos.",
            "No estás sola en esta guardia:\n\n• Dibu y Cuti están encerrados en la clínica: liberalos con [E] para que te ayuden a distraer y frenar a los corruptos.\n• Si alguno se corrompe, sedalo con tus dardos para curarlo.\n• Si te lastiman, usá un botiquín con la tecla [Q]."
        };

        if (_introAccentStripe != null) _introAccentStripe.color = accents[_introSlideIndex];
        if (_introStepText != null)
        {
            _introStepText.text = steps[_introSlideIndex];
            _introStepText.color = accents[_introSlideIndex];
        }
        if (_introSlideTitleText != null)
        {
            _introSlideTitleText.text = titles[_introSlideIndex];
            _introSlideTitleText.color = accents[_introSlideIndex];
        }
        if (_introSlideBodyText != null)
            _introSlideBodyText.text = bodies[_introSlideIndex];

        if (_introNextBtnLabel != null)
            _introNextBtnLabel.text = _introSlideIndex < 2 ? "SIGUIENTE  →  (Enter)" : "▶  ¡COMENZAR RESCATE! (Enter)";

        if (_introSkipButtonObj != null)
            _introSkipButtonObj.SetActive(_introSlideIndex < 2);

        if (_introFadeCoroutine != null)
            StopCoroutine(_introFadeCoroutine);
        _introFadeCoroutine = StartCoroutine(FadeInCurrentSlideRoutine());
    }

    private IEnumerator FadeInCurrentSlideRoutine()
    {
        if (_introSlideGroup == null) yield break;

        _introSlideGroup.alpha = 0f;
        float t = 0f;
        float dur = 0.25f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _introSlideGroup.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }
        _introSlideGroup.alpha = 1f;
    }

    public void StartOrResumeGame()
    {
        if (_introFadeCoroutine != null)
        {
            StopCoroutine(_introFadeCoroutine);
            _introFadeCoroutine = null;
        }

        if (_titleMenuPanel != null)
            _titleMenuPanel.SetActive(false);
        if (_optionsSubPanel != null)
            _optionsSubPanel.SetActive(false);
        if (_introStoryPanel != null)
            _introStoryPanel.SetActive(false);
        if (_pauseMenuPanel != null)
            _pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        ShowNotification("¡Rescatá la clínica! Sedá a los gatos corruptos y abrí todas las jaulas", new Color(0.75f, 0.95f, 1f));
    }

    public void OpenPauseMenu()
    {
        if (_pauseMenuPanel == null) return;
        _pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeFromPause()
    {
        if (_optionsSubPanel != null)
            _optionsSubPanel.SetActive(false);
        if (_pauseMenuPanel != null)
            _pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ToggleOptionsPanel()
    {
        if (_optionsSubPanel == null) return;
        bool nextState = !_optionsSubPanel.activeSelf;
        if (nextState && AudioManager.Instance != null)
        {
            if (_titleVolumeSlider != null)
                _titleVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            if (_volumeLabelText != null)
                _volumeLabelText.text = $"{Mathf.RoundToInt(AudioManager.Instance.MasterVolume * 100f)}%";

            if (_titleMusicVolumeSlider != null)
                _titleMusicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (_musicVolumeLabelText != null)
                _musicVolumeLabelText.text = $"{Mathf.RoundToInt(AudioManager.Instance.MusicVolume * 100f)}%";

            if (_titleSfxVolumeSlider != null)
                _titleSfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            if (_sfxVolumeLabelText != null)
                _sfxVolumeLabelText.text = $"{Mathf.RoundToInt(AudioManager.Instance.SfxVolume * 100f)}%";
        }
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

        int pct = Mathf.RoundToInt(value * 100f);
        if (_volumeLabelText != null)
            _volumeLabelText.text = $"{pct}%";
        if (_pauseVolumeLabelText != null)
            _pauseVolumeLabelText.text = $"Volumen General: {pct}%";

        if (_titleVolumeSlider != null && !Mathf.Approximately(_titleVolumeSlider.value, value))
            _titleVolumeSlider.SetValueWithoutNotify(value);
        if (_pauseVolumeSlider != null && !Mathf.Approximately(_pauseVolumeSlider.value, value))
            _pauseVolumeSlider.SetValueWithoutNotify(value);
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        if (_musicVolumeLabelText != null)
            _musicVolumeLabelText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void OnSfxVolumeSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
            // Toco un sonidito suave cada tanto mientras arrastrás el slider así escuchás el volumen real de los SFX
            if (Time.unscaledTime - _lastSfxPreviewTime >= 0.14f)
            {
                _lastSfxPreviewTime = Time.unscaledTime;
                AudioManager.Instance.PlayUIHover();
            }
        }

        if (_sfxVolumeLabelText != null)
            _sfxVolumeLabelText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    public void RestartLevel()
    {
        _hasShownTitleThisSession = true; // Reinicia directo a jugar sin pasar por la portada
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        _hasShownTitleThisSession = false; // Al recargar la escena abre la portada principal con todo el nivel reseteado
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Construcción del HUD, menú de inicio y menú de pausa
    private void BuildHUD()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        Transform existingCanvasTr = transform.Find("HUD_Canvas");
        if (existingCanvasTr == null)
        {
            GameObject existingInScene = GameObject.Find("HUD_Canvas");
            if (existingInScene != null)
                existingCanvasTr = existingInScene.transform;
        }

        GameObject canvasObj;
        if (existingCanvasTr != null)
        {
            canvasObj = existingCanvasTr.gameObject;
            canvasObj.transform.SetParent(transform, false);
        }
        else
        {
            canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);
        }

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
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

        Transform existingFill = hbBg.Find("HealthBar_Fill");
        GameObject fillObj = existingFill != null ? existingFill.gameObject : new GameObject("HealthBar_Fill");
        if (existingFill == null)
        {
            fillObj.transform.SetParent(hbBg, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
        }
        _playerHealthFill = fillObj.GetComponent<Image>();
        if (_playerHealthFill == null) _playerHealthFill = fillObj.AddComponent<Image>();
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
            "WASD: Mover  |  Click Der + Izq: Dardo Sedante  |  Espacio: Empujón Melee  |  E: Jaula / Botiquín  |  Q: Curarse  |  R: Reiniciar  |  ESC: Pausa";

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
        _notificationGroup = notifPanel.gameObject.GetComponent<CanvasGroup>();
        if (_notificationGroup == null) _notificationGroup = notifPanel.gameObject.AddComponent<CanvasGroup>();
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

        CreateButton(endRect, "Btn_Menu", defaultFont, "MENÚ PRINCIPAL",
            new Vector2(155f, -120f), new Vector2(260f, 58f), new Color(0.25f, 0.42f, 0.68f), ReturnToMainMenu);

        _endScreenPanel.SetActive(false);

        // 5. PANTALLA DE GAME OVER ILUSTRADA (Assets/GameOver)
        BuildGameOverScreen(canvasObj.transform, defaultFont);

        // 5b. PANTALLA DE VICTORIA ILUSTRADA (Assets/Victory)
        BuildVictoryScreen(canvasObj.transform, defaultFont);

        // 6. MENÚ DE PAUSA INDEPENDIENTE (ESC)
        BuildPauseMenu(canvasObj.transform, defaultFont);

        // 7. PANTALLA DE HISTORIA / INFORME DE EMERGENCIA (Al darle Jugar desde el Menú)
        BuildIntroStoryPanel(canvasObj.transform, defaultFont);

        // 8. PANTALLA DE MENÚ DE INICIO ILUSTRADA (Fondo Recepción Magenta + Logo + 4 Botones con Hover 110% Turquesa)
        BuildTitleMenu(canvasObj.transform, defaultFont);

        // 9. PANTALLA COMPLETA DE OPCIONES (como hija directa del Canvas para poder abrirla tanto desde Inicio como desde Pausa)
        BuildOptionsScreen(canvasObj.transform, defaultFont);
    }

    private void BuildGameOverScreen(Transform canvasTransform, Font font)
    {
        RectTransform goRect = CreatePanel(canvasTransform, "Panel_PantallaGameOver",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white);
        _gameOverScreenPanel = goRect.gameObject;

        Image bgImg = goRect.GetComponent<Image>();
        if (_gameOverBackgroundSprite != null)
        {
            bgImg.sprite = _gameOverBackgroundSprite;
            bgImg.color = Color.white;
            bgImg.preserveAspect = false;
        }
        else
        {
            bgImg.color = new Color(0.14f, 0.04f, 0.16f, 0.98f);
        }

        // 1) Botón "Reintentar" (Dorado/Ámbar con huellas):
        //    - Hover (110%): Dorado solar brillante (#FFC92E) manteniendo huellas e ícono/texto nítidos
        //    - Click/Presionado (105%): Naranja ámbar profundo (#EB8214)
        CreateSpriteMenuButton(
            goRect,
            "Btn_GameOver_Reintentar",
            _btnReintentarGameOverSprite,
            font,
            "↻  Reintentar",
            new Vector2(28f, -142f),
            new Vector2(560f, 187f),
            RestartLevel,
            new Color(1.00f, 0.79f, 0.18f),
            new Color(0.92f, 0.51f, 0.08f)
        );

        // 2) Botón "Menú Principal" (Rosa/Neón):
        //    - Hover (110%): Rosa/Magenta neón intenso (#FF6BE8) manteniendo ícono de casita y texto negros
        //    - Click/Presionado (105%): Magenta violeta profundo (#CC2EA8)
        CreateSpriteMenuButton(
            goRect,
            "Btn_GameOver_MenuPrincipal",
            _btnMenuPrincipalGameOverSprite,
            font,
            "⌂  Menú Principal",
            new Vector2(28f, -252f),
            new Vector2(495f, 165f),
            ReturnToMainMenu,
            new Color(1.00f, 0.42f, 0.91f),
            new Color(0.80f, 0.18f, 0.66f)
        );

        _gameOverScreenPanel.SetActive(false);
    }

    private void BuildVictoryScreen(Transform canvasTransform, Font font)
    {
        RectTransform vicRect = CreatePanel(canvasTransform, "Panel_PantallaVictoria",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white);
        _victoryScreenPanel = vicRect.gameObject;

        Image bgImg = vicRect.GetComponent<Image>();
        if (_victoryBackgroundSprite != null)
        {
            bgImg.sprite = _victoryBackgroundSprite;
            bgImg.color = Color.white;
            bgImg.preserveAspect = false;
        }
        else
        {
            bgImg.color = new Color(0.06f, 0.16f, 0.12f, 0.98f);
        }

        // Botón izquierdo "Jugar de nuevo" (crema/beige -> Hover turquesa brillante, Click turquesa oscuro, igual que el Menú Principal)
        CreateSpriteMenuButton(
            vicRect,
            "Btn_Victoria_JugarDeNuevo",
            _btnJugarDeNuevoVictorySprite,
            font,
            "↻  Jugar de nuevo",
            new Vector2(-240f, -466f),
            new Vector2(456f, 152f),
            RestartLevel,
            new Color(0.38f, 1.00f, 0.88f),
            new Color(0.18f, 0.83f, 0.71f)
        );

        // Botón derecho "Menú Principal" (dorado -> Hover dorado brillante, Click naranja ámbar intenso)
        CreateSpriteMenuButton(
            vicRect,
            "Btn_Victoria_MenuPrincipal",
            _btnMenuPrincipalVictorySprite,
            font,
            "⌂  Menú Principal",
            new Vector2(236f, -466f),
            new Vector2(422f, 141f),
            ReturnToMainMenu,
            new Color(1.00f, 0.88f, 0.25f),
            new Color(0.92f, 0.52f, 0.08f)
        );

        _victoryScreenPanel.SetActive(false);
    }

    private void BuildTitleMenu(Transform canvasTransform, Font font)
    {
        // Contenedor pantalla completa (1920x1080)
        RectTransform bgRect = CreatePanel(canvasTransform, "Panel_PantallaTitulo",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white);
        _titleMenuPanel = bgRect.gameObject;

        Image bgImg = bgRect.GetComponent<Image>();
        if (_menuBackgroundSprite != null)
        {
            bgImg.sprite = _menuBackgroundSprite;
            bgImg.color = Color.white;
            bgImg.preserveAspect = false;
        }
        else
        {
            bgImg.color = new Color(0.08f, 0.04f, 0.12f, 0.98f);
        }

        // LOGO OFICIAL ("Logo de Rescate Veterinario.png") arriba a la izquierda sobre la columna de botones
        Transform existingLogo = bgRect.Find("Logo_RescateVeterinario");
        GameObject logoObj = existingLogo != null ? existingLogo.gameObject : new GameObject("Logo_RescateVeterinario");
        if (existingLogo == null)
        {
            logoObj.transform.SetParent(bgRect, false);
            RectTransform logoRect = logoObj.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = new Vector2(-575f, 290f);
            logoRect.sizeDelta = new Vector2(900f, 450f);
        }

        Image logoImg = logoObj.GetComponent<Image>();
        if (logoImg == null) logoImg = logoObj.AddComponent<Image>();
        logoImg.raycastTarget = false; // Para que sus esquinas transparentes nunca tapen el botón Jugar
        if (_menuLogoSprite != null)
        {
            logoImg.sprite = _menuLogoSprite;
            logoImg.preserveAspect = true;
            logoImg.color = Color.white;
        }

        // COLUMNA DE 4 BOTONES CON SPRITES REALES Y HOVER TURQUESA (110% Hover / 105% Presionado)
        Vector2 btnSize = new Vector2(430f, 138f);
        float columnX = -585f;

        CreateSpriteMenuButton(bgRect, "Btn_Jugar", _btnJugarSprite, font, "▶  Jugar",
            new Vector2(columnX, -35f), btnSize, OnPlayButtonPressed);

        CreateSpriteMenuButton(bgRect, "Btn_Opciones", _btnOpcionesSprite, font, "⚙  Opciones",
            new Vector2(columnX, -148f), btnSize, ToggleOptionsPanel);

        CreateSpriteMenuButton(bgRect, "Btn_Creditos", _btnCreditosSprite, font, "★  Créditos",
            new Vector2(columnX, -261f), btnSize, ToggleCreditsPanel);

        CreateSpriteMenuButton(bgRect, "Btn_Salir", _btnSalirSprite, font, "✖  Salir",
            new Vector2(columnX, -374f), btnSize, QuitGame);

        // Si quedó un SubPanel_Opciones o Panel_PantallaOpciones viejo adentro del título, lo apago
        Transform oldOpt = bgRect.Find("SubPanel_Opciones");
        if (oldOpt != null) oldOpt.gameObject.SetActive(false);
        Transform oldFullOpt = bgRect.Find("Panel_PantallaOpciones");
        if (oldFullOpt != null) oldFullOpt.gameObject.SetActive(false);

        // PANTALLA COMPLETA DE CRÉDITOS ILUSTRADA (Fondo Pizarra de Corcho con Dibu y Cuti + Botón "<- Volver" abajo a la izquierda)
        RectTransform credRect = CreatePanel(bgRect, "Panel_PantallaCreditos",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white);
        _creditsSubPanel = credRect.gameObject;

        Image credBgImg = credRect.GetComponent<Image>();
        if (_creditsBackgroundSprite != null)
        {
            credBgImg.sprite = _creditsBackgroundSprite;
            credBgImg.color = Color.white;
            credBgImg.preserveAspect = false;
        }
        else
        {
            credBgImg.color = new Color(0.08f, 0.05f, 0.14f, 0.98f);
        }

        // Botón "<- Volver" con papel rasgado ubicado abajo a la izquierda sobre el escritorio y con el mismo Hover 110% turquesa y Presión 105%
        CreateSpriteMenuButton(credRect, "Btn_VolverCreditos", _btnVolverSprite, font, "⬅  Volver",
            new Vector2(-745f, -450f), new Vector2(348f, 116f), () => _creditsSubPanel.SetActive(false));

        _creditsSubPanel.SetActive(false);
        _titleMenuPanel.SetActive(false);
    }

    private void BuildOptionsScreen(Transform canvasTransform, Font font)
    {
        // PANTALLA COMPLETA DE OPCIONES ILUSTRADA (Fondo Pizarra de Corcho + 3 Sliders + Botón "<- Volver" abajo a la izquierda)
        RectTransform optRect = CreatePanel(canvasTransform, "Panel_PantallaOpciones",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white);
        _optionsSubPanel = optRect.gameObject;

        Image optBgImg = optRect.GetComponent<Image>();
        if (_optionsBackgroundSprite != null)
        {
            optBgImg.sprite = _optionsBackgroundSprite;
            optBgImg.color = Color.white;
            optBgImg.preserveAspect = false;
        }
        else
        {
            optBgImg.color = new Color(0.08f, 0.05f, 0.14f, 0.98f);
        }

        float currentMaster = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : AudioListener.volume;
        float currentMusic = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f;
        float currentSfx = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;

        Color percentColor = new Color(0.30f, 0.11f, 0.36f, 1f);
        Vector2 sliderSize = new Vector2(496f, 36f);

        // 1) Volumen General (adentro de la primera cápsula punteada)
        _volumeLabelText = CreateText(optRect, "Opt_MasterPct", font, 28, FontStyle.Bold, TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(218f, 70f), new Vector2(180f, 38f), percentColor);
        _volumeLabelText.text = $"{Mathf.RoundToInt(currentMaster * 100f)}%";
        _titleVolumeSlider = CreateOptionsSlider(optRect, "Slider_VolumenGeneral", currentMaster,
            new Vector2(60f, 22f), sliderSize, new Color(0.18f, 0.84f, 0.74f, 1f), OnVolumeSliderChanged);

        // 2) Música (adentro de la segunda cápsula punteada)
        _musicVolumeLabelText = CreateText(optRect, "Opt_MusicPct", font, 28, FontStyle.Bold, TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(218f, -60f), new Vector2(180f, 38f), percentColor);
        _musicVolumeLabelText.text = $"{Mathf.RoundToInt(currentMusic * 100f)}%";
        _titleMusicVolumeSlider = CreateOptionsSlider(optRect, "Slider_VolumenMusica", currentMusic,
            new Vector2(60f, -108f), sliderSize, new Color(0.98f, 0.72f, 0.16f, 1f), OnMusicVolumeSliderChanged);

        // 3) Efectos SFX (adentro de la tercera cápsula punteada)
        _sfxVolumeLabelText = CreateText(optRect, "Opt_SfxPct", font, 28, FontStyle.Bold, TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(218f, -190f), new Vector2(180f, 38f), percentColor);
        _sfxVolumeLabelText.text = $"{Mathf.RoundToInt(currentSfx * 100f)}%";
        _titleSfxVolumeSlider = CreateOptionsSlider(optRect, "Slider_VolumenSFX", currentSfx,
            new Vector2(60f, -239f), sliderSize, new Color(0.92f, 0.28f, 0.58f, 1f), OnSfxVolumeSliderChanged);

        // Botón "<- Volver" con papel rasgado abajo a la izquierda, igual que en Créditos
        CreateSpriteMenuButton(optRect, "Btn_VolverOpciones", _btnVolverSprite, font, "⬅  Volver",
            new Vector2(-745f, -450f), new Vector2(348f, 116f), () => _optionsSubPanel.SetActive(false));

        _optionsSubPanel.SetActive(false);
    }

    private void BuildPauseMenu(Transform canvasTransform, Font font)
    {
        // Velo oscuro violáceo translúcido para que se vea la clínica congelada atrás sin confundirse con el Menú Principal
        RectTransform pauseOverlay = CreatePanel(canvasTransform, "Panel_MenuPausa",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.06f, 0.03f, 0.11f, 0.74f));
        _pauseMenuPanel = pauseOverlay.gameObject;
        pauseOverlay.GetComponent<Image>().color = new Color(0.06f, 0.03f, 0.11f, 0.74f);

        // Si quedó la cajita gris vieja o botones viejos guardados en la escena, los apago
        Transform oldCard = pauseOverlay.Find("Card_Pausa");
        if (oldCard != null) oldCard.gameObject.SetActive(false);
        Transform oldReintentar = pauseOverlay.Find("Btn_Pause_Reintentar");
        if (oldReintentar != null) oldReintentar.gameObject.SetActive(false);
        Transform oldSalir = pauseOverlay.Find("Btn_Pause_Salir");
        if (oldSalir != null) oldSalir.gameObject.SetActive(false);

        // Logo reciclado centrado (un poco más abajo para no pisar el banner de notificaciones de arriba)
        Transform existingLogo = pauseOverlay.Find("Logo_Pausa");
        GameObject logoObj = existingLogo != null ? existingLogo.gameObject : new GameObject("Logo_Pausa");
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        if (existingLogo == null)
        {
            logoObj.transform.SetParent(pauseOverlay, false);
            logoRect = logoObj.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
        }
        logoRect.anchoredPosition = new Vector2(0f, 255f);
        logoRect.sizeDelta = new Vector2(540f, 270f);

        Image logoImg = logoObj.GetComponent<Image>();
        if (logoImg == null) logoImg = logoObj.AddComponent<Image>();
        logoImg.raycastTarget = false;
        if (_menuLogoSprite != null)
        {
            logoImg.sprite = _menuLogoSprite;
            logoImg.preserveAspect = true;
            logoImg.color = Color.white;
        }

        Text pauseSub = CreateText(pauseOverlay, "Pause_Subtitle", font, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 102f), new Vector2(600f, 38f),
            new Color(0.52f, 0.98f, 0.90f, 0.95f));
        pauseSub.text = "JUEGO EN PAUSA  ·  ESC para volver";
        pauseSub.rectTransform.anchoredPosition = new Vector2(0f, 102f);

        // Solo 3 botones bien proporcionados visualmente entre sí: Volver, Opciones y Menú Principal
        CreateSpriteMenuButton(pauseOverlay, "Btn_Pause_Volver", _btnVolverSprite, font, "⬅  Volver",
            new Vector2(0f, 2f), new Vector2(368f, 118f), ResumeFromPause);

        CreateSpriteMenuButton(pauseOverlay, "Btn_Pause_Opciones", _btnOpcionesSprite, font, "⚙  Opciones",
            new Vector2(0f, -122f), new Vector2(416f, 134f), ToggleOptionsPanel);

        CreateSpriteMenuButton(pauseOverlay, "Btn_Pause_MenuPrincipal", _btnMenuPrincipalVictorySprite, font, "⌂  Menú Principal",
            new Vector2(0f, -246f), new Vector2(388f, 126f), ReturnToMainMenu,
            new Color(1.00f, 0.88f, 0.25f),
            new Color(0.92f, 0.52f, 0.08f));

        _pauseMenuPanel.SetActive(false);
    }

    private void BuildIntroStoryPanel(Transform canvasTransform, Font font)
    {
        // Fondo ilustrado oscurecido para que la diapositiva clínica resalte sobre la recepción magenta
        RectTransform root = CreatePanel(canvasTransform, "Panel_HistoriaIntro",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.05f, 0.03f, 0.09f, 0.96f));
        _introStoryPanel = root.gameObject;

        Sprite bg = _introBackgroundSprite != null ? _introBackgroundSprite : _menuBackgroundSprite;
        if (bg != null)
        {
            Image bgImg = root.GetComponent<Image>();
            bgImg.sprite = bg;
            bgImg.color = new Color(0.28f, 0.22f, 0.36f, 1f);
            bgImg.preserveAspect = false;
        }

        // Tarjeta / Ficha Clínica central de la diapositiva actual
        RectTransform dossier = CreatePanel(root, "Ficha_InformeEmergencia",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1040f, 600f), new Color(0.08f, 0.06f, 0.14f, 0.96f));

        RectTransform stripe = CreatePanel(dossier, "Header_Stripe",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 8f), new Color(0.96f, 0.35f, 0.55f, 1f));
        _introAccentStripe = stripe.GetComponent<Image>();

        // Contenido animado de cada diapositiva
        Transform existingSlideContent = dossier.Find("Slide_Content");
        GameObject slideContent = existingSlideContent != null ? existingSlideContent.gameObject : new GameObject("Slide_Content");
        RectTransform contentRect = slideContent.GetComponent<RectTransform>();
        if (existingSlideContent == null)
        {
            slideContent.transform.SetParent(dossier, false);
            contentRect = slideContent.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
        }
        _introSlideGroup = slideContent.GetComponent<CanvasGroup>();
        if (_introSlideGroup == null) _introSlideGroup = slideContent.AddComponent<CanvasGroup>();

        _introStepText = CreateText(contentRect, "Intro_Step", font, 19, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), new Vector2(-60f, 32f),
            new Color(0.96f, 0.35f, 0.55f));

        _introSlideTitleText = CreateText(contentRect, "Intro_SlideTitle", font, 40, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -104f), new Vector2(-80f, 54f),
            Color.white);

        _introSlideBodyText = CreateText(contentRect, "Intro_SlideBody", font, 25, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 15f), new Vector2(-140f, -250f),
            new Color(0.93f, 0.95f, 0.99f));

        // Botón principal: en slide 1 y 2 dice "SIGUIENTE ->", en slide 3 dice "¡COMENZAR RESCATE!"
        RectTransform nextBtnRect = CreatePanel(dossier, "Btn_SiguienteIntro",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-115f, -225f), new Vector2(390f, 62f), new Color(0.20f, 0.82f, 0.68f));
        Button nextBtn = nextBtnRect.gameObject.GetComponent<Button>();
        if (nextBtn == null) nextBtn = nextBtnRect.gameObject.AddComponent<Button>();
        nextBtn.transition = Selectable.Transition.None;
        nextBtn.targetGraphic = nextBtnRect.GetComponent<Image>();
        nextBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.AddListener(AdvanceIntroSlide);
        if (nextBtnRect.gameObject.GetComponent<MenuButtonHover>() == null)
            nextBtnRect.gameObject.AddComponent<MenuButtonHover>();
        _introNextBtnLabel = CreateText(nextBtnRect, "Label", font, 21, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);

        // Botón "SALTAR": salta directo al juego (se oculta en la diapositiva 3 porque ahí ya está "¡Comenzar Rescate!")
        RectTransform skipBtnRect = CreatePanel(dossier, "Btn_SaltarIntro",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(225f, -225f), new Vector2(210f, 62f), new Color(0.25f, 0.24f, 0.36f));
        Button skipBtn = skipBtnRect.gameObject.GetComponent<Button>();
        if (skipBtn == null) skipBtn = skipBtnRect.gameObject.AddComponent<Button>();
        skipBtn.transition = Selectable.Transition.None;
        skipBtn.targetGraphic = skipBtnRect.GetComponent<Image>();
        skipBtn.onClick.RemoveAllListeners();
        skipBtn.onClick.AddListener(StartOrResumeGame);
        if (skipBtnRect.gameObject.GetComponent<MenuButtonHover>() == null)
            skipBtnRect.gameObject.AddComponent<MenuButtonHover>();
        CreateText(skipBtnRect, "Label", font, 20, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white).text = "SALTAR  ⏭";
        _introSkipButtonObj = skipBtnRect.gameObject;

        _introStoryPanel.SetActive(false);
    }

    private void CreateSpriteMenuButton(Transform parent, string name, Sprite sprite, Font fallbackFont, string fallbackLabel, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        CreateSpriteMenuButton(
            parent, name, sprite, fallbackFont, fallbackLabel, anchoredPos, size, onClick,
            new Color(0.38f, 1.00f, 0.88f),
            new Color(0.18f, 0.83f, 0.71f)
        );
    }

    private void CreateSpriteMenuButton(Transform parent, string name, Sprite sprite, Font fallbackFont, string fallbackLabel, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick, Color hoverTint, Color pressedTint)
    {
        Transform existing = parent.Find(name);
        GameObject btnObj = existing != null ? existing.gameObject : new GameObject(name);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (existing == null)
        {
            btnObj.transform.SetParent(parent, false);
            rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        Image img = btnObj.GetComponent<Image>();
        if (img == null) img = btnObj.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else if (existing == null)
        {
            img.color = new Color(0.88f, 0.80f, 0.77f, 1f);
            CreateText(rt, "Label", fallbackFont, 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black).text = fallbackLabel;
        }

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();
        // Apago la transición por defecto de Unity para que MenuButtonHover maneje el cambio de color y el escalado 110% / 105%
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);

        MenuButtonHover hover = btnObj.GetComponent<MenuButtonHover>();
        if (hover == null) hover = btnObj.AddComponent<MenuButtonHover>();
        if (sprite != null)
            hover.SetBaseSprite(sprite, hoverTint, pressedTint);
    }

    private Slider CreateVolumeSlider(Transform parent, float initialValue, Vector2 anchoredPos)
    {
        Transform existing = parent.Find("Slider_Volumen");
        if (existing != null)
        {
            Slider existingSlider = existing.GetComponent<Slider>();
            if (existingSlider != null)
            {
                existingSlider.SetValueWithoutNotify(initialValue);
                existingSlider.onValueChanged.RemoveAllListeners();
                existingSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
                return existingSlider;
            }
        }

        GameObject sliderObj = new GameObject("Slider_Volumen");
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(340f, 36f);
        sliderRect.anchoredPosition = anchoredPos;

        // Fondo de la barra
        CreatePanel(sliderRect, "Background",
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
            Vector2.zero, Vector2.zero, new Color(0.38f, 1.00f, 0.88f, 1f));

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
        return slider;
    }

    // Armo cada slider de la pantalla de Opciones adentro de las cápsulas punteadas del fondo ilustrado
    private Slider CreateOptionsSlider(Transform parent, string name, float initialValue, Vector2 anchoredPos, Vector2 size, Color fillColor, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Slider existingSlider = existing.GetComponent<Slider>();
            if (existingSlider != null)
            {
                existingSlider.SetValueWithoutNotify(initialValue);
                existingSlider.onValueChanged.RemoveAllListeners();
                existingSlider.onValueChanged.AddListener(onValueChanged);
                return existingSlider;
            }
        }

        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = size;
        sliderRect.anchoredPosition = anchoredPos;

        // Fondo translúcido violeta suave para que acompañe la cápsula dibujada
        CreatePanel(sliderRect, "Background",
            new Vector2(0f, 0.22f), new Vector2(1f, 0.78f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.35f, 0.14f, 0.38f, 0.28f));

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderRect, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.22f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.78f);
        fillAreaRect.offsetMin = new Vector2(6f, 0f);
        fillAreaRect.offsetMax = new Vector2(-6f, 0f);

        RectTransform fill = CreatePanel(fillAreaRect, "Fill",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, fillColor);

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderRect, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(18f, 0f);
        handleAreaRect.offsetMax = new Vector2(-18f, 0f);

        RectTransform handle = CreatePanel(handleAreaRect, "Handle",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(36f, 36f), Color.white);
        Image handleImg = handle.GetComponent<Image>();
        handleImg.sprite = GetOrCreateSliderKnobSprite();

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(onValueChanged);
        return slider;
    }

    // Genero un circulito con borde violeta oscuro en memoria para el tirador de los sliders de Opciones
    private static Sprite GetOrCreateSliderKnobSprite()
    {
        if (_sliderKnobSprite != null) return _sliderKnobSprite;

        const int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float outerR = size * 0.46f;
        float innerR = size * 0.34f;
        Color borderCol = new Color(0.30f, 0.11f, 0.36f, 1f);
        Color fillCol = new Color(1f, 0.98f, 0.94f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d <= innerR)
                    tex.SetPixel(x, y, fillCol);
                else if (d <= outerR)
                    tex.SetPixel(x, y, borderCol);
                else if (d <= outerR + 1.2f)
                    tex.SetPixel(x, y, new Color(borderCol.r, borderCol.g, borderCol.b, Mathf.Clamp01(outerR + 1.2f - d)));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();

        _sliderKnobSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _sliderKnobSprite;
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            RectTransform existingRt = existing.GetComponent<RectTransform>();
            if (existing.GetComponent<Image>() == null)
                existing.gameObject.AddComponent<Image>().color = bgColor;
            return existingRt;
        }

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
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Text existingTxt = existing.GetComponent<Text>();
            if (existingTxt != null) return existingTxt;
        }

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

        Button btn = btnRect.gameObject.GetComponent<Button>();
        if (btn == null) btn = btnRect.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = btnRect.GetComponent<Image>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);

        if (btnRect.gameObject.GetComponent<MenuButtonHover>() == null)
            btnRect.gameObject.AddComponent<MenuButtonHover>();

        CreateText(btnRect, "Label", font, 20, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white).text = label;
    }

#if UNITY_EDITOR
    [ContextMenu("Crear o Actualizar Pantallas UI en Escena")]
    public void BuildSceneUIInEditor()
    {
        LoadMenuSpritesIfNeeded();
        BuildHUD();
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }

    [UnityEditor.MenuItem("Rescate Veterinario/Crear o Actualizar Pantallas UI en Escena")]
    public static void CreateSceneUIMenuCommand()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject go = new GameObject("GameManager_HUD");
            gm = go.AddComponent<GameManager>();
        }
        gm.BuildSceneUIInEditor();
        UnityEditor.Selection.activeGameObject = gm.gameObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
#endif
}

#if UNITY_EDITOR
// Al generar la Build de Windows, copia automáticamente las carpetas Assets/MainMenu y Assets/Creditos
// adentro de la carpeta _Data de la build para que File.ReadAllBytes encuentre todos los PNGs del menú.
public class MenuBuildAssetsPostprocessor : UnityEditor.Build.IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        string exePath = report.summary.outputPath;
        if (string.IsNullOrEmpty(exePath)) return;

        string buildDir = Path.GetDirectoryName(exePath);
        string exeName = Path.GetFileNameWithoutExtension(exePath);
        string dataDir = Path.Combine(buildDir, exeName + "_Data");
        if (!Directory.Exists(dataDir)) return;

        CopyPngFolder("MainMenu", dataDir);
        CopyPngFolder("Creditos", dataDir);
        CopyPngFolder("Options", dataDir);
        CopyPngFolder("GameOver", dataDir);
        CopyPngFolder("Victory", dataDir);
    }

    private static void CopyPngFolder(string folderName, string dataDir)
    {
        string srcDir = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(srcDir)) return;

        string dstDir = Path.Combine(dataDir, folderName);
        Directory.CreateDirectory(dstDir);

        foreach (string file in Directory.GetFiles(srcDir, "*.png"))
        {
            string destFile = Path.Combine(dstDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
    }
}
#endif
