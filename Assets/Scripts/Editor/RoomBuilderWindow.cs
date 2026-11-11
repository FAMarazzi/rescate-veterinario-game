/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 10/30/2026, 11:27:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 9:09:22 AM
 * @Description: Generador completo de niveles: Pisos en matriz, Paredes perimetrales, Habitaciones interiores con puertas
 *   y Decoración automática de cada sala de la clínica (Recepción, Farmacia, Consultorios, Sala de Jaulas y Quirófano).
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEditor;
using UnityEngine;

public class RoomBuilderWindow : EditorWindow
{
    private enum Tab { Piso, ParedesPerimetro, HabitacionesInteriores, Decoracion, Enemigos }
    private Tab _currentTab = Tab.Piso;

    [Header("Configuración General")]
    private int _columns = 20;
    private int _rows = 20;
    private float _tileSize = 1.885f;

    [Header("Prefabs")]
    private GameObject _tilePrefab;
    private GameObject _wallPrefab;
    private GameObject _doorWallPrefab;

    [Header("Prefabs / Modelos de Decoración")]
    private GameObject _receptionDeskPrefab;
    private GameObject _examTablePrefab;
    private GameObject _medicalCabinetPrefab;
    private GameObject _medicalCartPrefab;
    private GameObject _cagePrefab;
    private GameObject _corruptCatPrefab;

    // Alturas en metros pensadas respecto a las paredes de la clínica (~2.1m) y la veterinaria (~1.6m)
    private float _deskHeight = 1.25f;
    private float _tableHeight = 1.35f;
    private float _cabinetHeight = 2.05f;
    private float _cartHeight = 1.25f;
    private float _cageHeight = 1.65f;
    private float _propScaleMultiplier = 1.25f;
    private Vector3 _propBaseRotation = new Vector3(-90f, 0f, 0f);

    [Header("Opciones Piso")]
    private bool _randomRotation = true;

    [Header("Opciones Perímetro")]
    private bool _addPerimeterDoors = true;
    private int _doorsPerSide = 1;

    [Header("Opciones Habitaciones Interiores")]
    private int _roomGridStep = 5; // Tamaño de cada consultorio en baldosas (ej: cada 5 baldosas una pared)
    [Range(0.1f, 0.8f)] private float _doorwayChance = 0.35f; // Probabilidad de que un módulo de pared sea puerta

    [MenuItem("Tools/Generador de Nivel")]
    public static void ShowWindow()
    {
        GetWindow<RoomBuilderWindow>("Generador de Nivel");
    }

    private void OnEnable()
    {
        AutoFindFurnitureAssets();
    }

    private void OnGUI()
    {
        GUILayout.Label("Generador de Clínica Veterinaria", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, new string[] { "1. Piso", "2. Perímetro", "3. Salas", "4. Decoración", "5. Enemigos" });
        EditorGUILayout.Space(10);

        // Campos globales compartidos
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Dimensiones Globales del Mapa", EditorStyles.miniBoldLabel);
        _columns = EditorGUILayout.IntSlider("Columnas (X)", _columns, 4, 40);
        _rows = EditorGUILayout.IntSlider("Filas (Z)", _rows, 4, 40);
        _tileSize = EditorGUILayout.FloatField("Tamaño Baldosa / Pared (m)", _tileSize);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        switch (_currentTab)
        {
            case Tab.Piso:
                DrawFloorTab();
                break;
            case Tab.ParedesPerimetro:
                DrawPerimeterTab();
                break;
            case Tab.HabitacionesInteriores:
                DrawInteriorRoomsTab();
                break;
            case Tab.Decoracion:
                DrawDecorationTab();
                break;
            case Tab.Enemigos:
                DrawEnemiesTab();
                break;
        }

        EditorGUILayout.Space(12);
        if (GUILayout.Button("⚡ Optimizar Clínica en Escena (Subir FPS)", GUILayout.Height(30)))
        {
            OptimizeSceneClinic();
        }
    }

    private void DrawFloorTab()
    {
        GUILayout.Label("Piso Modular", EditorStyles.boldLabel);
        _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Prefab de Baldosa", _tilePrefab, typeof(GameObject), true);
        _randomRotation = EditorGUILayout.Toggle("Rotación Aleatoria (90°)", _randomRotation);

        EditorGUILayout.Space(15);
        if (GUILayout.Button("Generar Piso Completo", GUILayout.Height(35)))
        {
            GenerateFloor();
        }
    }

    private void DrawPerimeterTab()
    {
        GUILayout.Label("Paredes Exteriores (Perímetro 4 lados)", EditorStyles.boldLabel);
        _wallPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab de Pared", _wallPrefab, typeof(GameObject), true);
        _doorWallPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Pared con Puerta (Opcional)", _doorWallPrefab, typeof(GameObject), true);
        _addPerimeterDoors = EditorGUILayout.Toggle("Incluir Puertas en Perímetro", _addPerimeterDoors);
        if (_addPerimeterDoors)
        {
            _doorsPerSide = EditorGUILayout.IntSlider("Puertas por lado", _doorsPerSide, 1, 3);
        }

        EditorGUILayout.Space(15);
        if (GUILayout.Button("Construir Perímetro de Paredes", GUILayout.Height(35)))
        {
            GeneratePerimeter();
        }
    }

    private void DrawInteriorRoomsTab()
    {
        GUILayout.Label("División de Habitaciones / Consultorios", EditorStyles.boldLabel);
        _wallPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab de Pared", _wallPrefab, typeof(GameObject), true);
        _doorWallPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Pared con Puerta", _doorWallPrefab, typeof(GameObject), true);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🏥 PLANO REALISTA DE CLÍNICA (Recomendado)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Genera una clínica completa adaptada al mapa 20x20:", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("• Gran Recepción / Sala de Espera (12x8 baldosas)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Farmacia / Depósito de ítems (8x8 baldosas)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Pasillo central ancho distribuidor (4 baldosas)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Consultorios médicos 1 y 2", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Sala de Jaulas (rescate de animales)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Quirófano / Sala del Boss (al fondo)", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        if (GUILayout.Button("🏥 Generar Plano Realista de Clínica", GUILayout.Height(38)))
        {
            GenerateClinicLayout();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Distribución Modular Cuadriculada (Alternativa)", EditorStyles.boldLabel);
        _roomGridStep = EditorGUILayout.IntSlider("Tamaño de Salas (baldosas)", _roomGridStep, 3, 10);
        _doorwayChance = EditorGUILayout.Slider("Probabilidad de Puertas", _doorwayChance, 0.1f, 0.8f);
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Generar Cuadrícula Modular de Salas", GUILayout.Height(30)))
        {
            GenerateInteriorRooms();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawDecorationTab()
    {
        GUILayout.Label("Amoblar y Decorar Clínica Veterinaria", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Ubica automáticamente cada mueble según la lógica de cada sala (Recepción, Farmacia, Consultorios 1 y 2, Sala de Jaulas y Quirófano), les crea sus Materiales + BoxCollider y actualiza el NavMesh.",
            MessageType.Info);

        EditorGUILayout.Space(5);
        _receptionDeskPrefab = (GameObject)EditorGUILayout.ObjectField("1. Mostrador Recepción", _receptionDeskPrefab, typeof(GameObject), true);
        _examTablePrefab = (GameObject)EditorGUILayout.ObjectField("2. Camilla Veterinaria", _examTablePrefab, typeof(GameObject), true);
        _medicalCabinetPrefab = (GameObject)EditorGUILayout.ObjectField("3. Armario Médico", _medicalCabinetPrefab, typeof(GameObject), true);
        _medicalCartPrefab = (GameObject)EditorGUILayout.ObjectField("4. Carrito Médico", _medicalCartPrefab, typeof(GameObject), true);
        _cagePrefab = (GameObject)EditorGUILayout.ObjectField("5. Jaula (Kennel)", _cagePrefab, typeof(GameObject), true);

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Altura en Metros por Tipo de Mueble (Pared = 2.1m)", EditorStyles.miniBoldLabel);
        _deskHeight = EditorGUILayout.Slider("Altura Mostrador (m)", _deskHeight, 0.8f, 2.5f);
        _tableHeight = EditorGUILayout.Slider("Altura Camilla (m)", _tableHeight, 0.8f, 2.5f);
        _cabinetHeight = EditorGUILayout.Slider("Altura Armario (m)", _cabinetHeight, 1.2f, 3.0f);
        _cartHeight = EditorGUILayout.Slider("Altura Carrito (m)", _cartHeight, 0.8f, 2.2f);
        _cageHeight = EditorGUILayout.Slider("Altura Jaula (m)", _cageHeight, 1.0f, 2.5f);
        _propScaleMultiplier = EditorGUILayout.Slider("Multiplicador General", _propScaleMultiplier, 0.5f, 2.5f);
        _propBaseRotation = EditorGUILayout.Vector3Field("Rotación Base (X, Y, Z)", _propBaseRotation);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("🔍 Auto-Detectar los 5 Modelos en Assets/Models", GUILayout.Height(26)))
        {
            AutoFindFurnitureAssets(true);
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("🛋️ Amoblar Toda la Clínica Automáticamente", GUILayout.Height(38)))
        {
            if (_propBaseRotation == Vector3.zero)
                _propBaseRotation = new Vector3(-90f, 0f, 0f);
            GenerateClinicDecoration();
        }
    }

    // ==========================================
    // 1. GENERAR PISO
    // ==========================================
    private void GenerateFloor()
    {
        if (_tilePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Asigná el prefab de baldosa.", "OK");
            return;
        }

        GameObject parent = GetOrCreateParent("Clinica_Piso");
        ClearChildren(parent);

        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;
        Quaternion baseRot = _tilePrefab.transform.rotation;

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                Vector3 pos = new Vector3(startX + (c * _tileSize), 0f, startZ + (r * _tileSize));
                Quaternion rot = baseRot;
                if (_randomRotation)
                {
                    rot = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f) * baseRot;
                }

                SpawnPiece(_tilePrefab, pos, rot, parent.transform, $"Piso_{c}_{r}");
            }
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Éxito", $"¡Piso de {_columns}x{_rows} generado!", "OK");
    }

    // ==========================================
    // 2. GENERAR PERÍMETRO
    // ==========================================
    private void GeneratePerimeter()
    {
        if (_wallPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Asigná el prefab de pared.", "OK");
            return;
        }

        GameObject parent = GetOrCreateParent("Clinica_Perimetro");
        ClearChildren(parent);

        float halfW = (_columns * _tileSize) * 0.5f;
        float halfH = (_rows * _tileSize) * 0.5f;
        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;

        Quaternion baseRot = _wallPrefab.transform.rotation;

        // Paredes Norte (Z+) y Sur (Z-)
        for (int c = 0; c < _columns; c++)
        {
            float posX = startX + (c * _tileSize);
            bool isDoorN = _addPerimeterDoors && _doorWallPrefab != null && (c == _columns / 2);
            bool isDoorS = _addPerimeterDoors && _doorWallPrefab != null && (c == _columns / 2);

            // Borde Norte (mira hacia abajo / 180°)
            GameObject prefabN = isDoorN ? _doorWallPrefab : _wallPrefab;
            SpawnPiece(prefabN, new Vector3(posX, 0f, halfH), Quaternion.Euler(0f, 180f, 0f) * baseRot, parent.transform, $"Pared_Norte_{c}");

            // Borde Sur (mira hacia arriba / 0°)
            GameObject prefabS = isDoorS ? _doorWallPrefab : _wallPrefab;
            SpawnPiece(prefabS, new Vector3(posX, 0f, -halfH), Quaternion.Euler(0f, 0f, 0f) * baseRot, parent.transform, $"Pared_Sur_{c}");
        }

        // Paredes Este (X+) y Oeste (X-)
        for (int r = 0; r < _rows; r++)
        {
            float posZ = startZ + (r * _tileSize);
            bool isDoorE = _addPerimeterDoors && _doorWallPrefab != null && (r == _rows / 2);
            bool isDoorW = _addPerimeterDoors && _doorWallPrefab != null && (r == _rows / 2);

            // Borde Este (mira hacia la izquierda / 270°)
            GameObject prefabE = isDoorE ? _doorWallPrefab : _wallPrefab;
            SpawnPiece(prefabE, new Vector3(halfW, 0f, posZ), Quaternion.Euler(0f, 270f, 0f) * baseRot, parent.transform, $"Pared_Este_{r}");

            // Borde Oeste (mira hacia la derecha / 90°)
            GameObject prefabW = isDoorW ? _doorWallPrefab : _wallPrefab;
            SpawnPiece(prefabW, new Vector3(-halfW, 0f, posZ), Quaternion.Euler(0f, 90f, 0f) * baseRot, parent.transform, $"Pared_Oeste_{r}");
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Éxito", "¡Perímetro exterior construido!", "OK");
    }

    // ==========================================
    // 3A. GENERAR PLANO REALISTA DE CLÍNICA
    // ==========================================
    private void GenerateClinicLayout()
    {
        if (_wallPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Asigná el prefab de pared.", "OK");
            return;
        }

        GameObject parent = GetOrCreateParent("Clinica_SalasInteriores");
        ClearChildren(parent);

        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;
        Quaternion baseRot = _wallPrefab.transform.rotation;

        // Pared vertical entre columna c-1 y c en fila r
        void PutWallV(int c, int r, bool isDoor = false)
        {
            if (c <= 0 || c >= _columns || r < 0 || r >= _rows) return;
            float px = startX + (c * _tileSize) - (_tileSize * 0.5f);
            float pz = startZ + (r * _tileSize);
            GameObject prefab = (isDoor && _doorWallPrefab != null) ? _doorWallPrefab : _wallPrefab;
            SpawnPiece(prefab, new Vector3(px, 0f, pz), Quaternion.Euler(0f, 90f, 0f) * baseRot, parent.transform, $"Pared_V_{c}_{r}");
        }

        // Pared horizontal entre fila r-1 y r en columna c
        void PutWallH(int c, int r, bool isDoor = false)
        {
            if (r <= 0 || r >= _rows || c < 0 || c >= _columns) return;
            float px = startX + (c * _tileSize);
            float pz = startZ + (r * _tileSize) - (_tileSize * 0.5f);
            GameObject prefab = (isDoor && _doorWallPrefab != null) ? _doorWallPrefab : _wallPrefab;
            SpawnPiece(prefab, new Vector3(px, 0f, pz), Quaternion.Euler(0f, 0f, 0f) * baseRot, parent.transform, $"Pared_H_{c}_{r}");
        }

        // 1. ZONA SUR: Separación Recepción (izq) vs Farmacia/Depósito (der) en Col 12 (Filas 0 a 7)
        for (int r = 0; r < 8; r++)
        {
            bool isDoor = (r == 3); // Puerta entre Recepción y Farmacia
            PutWallV(12, r, isDoor);
        }

        // 2. PARED QUE SEPARA ZONA SUR DEL PASILLO (en Fila 8)
        for (int c = 0; c < _columns; c++)
        {
            // Dejamos libre el corredor central (Cols 8 a 11) para entrar desde Recepción al pasillo
            if (c >= 8 && c <= 11) continue;

            // Puerta que sale de Farmacia al pasillo en Col 15
            bool isDoor = (c == 15);
            PutWallH(c, 8, isDoor);
        }

        // 3. PASILLO CENTRAL (Cols 8 a 11): Paredes laterales del pasillo (Filas 8 a 19)
        // Pared Izquierda (Col 8) -> da a Consultorio 1 y 2
        for (int r = 8; r < _rows; r++)
        {
            bool isDoor = (r == 10 || r == 16); // Puertas a Consultorios 1 y 2
            PutWallV(8, r, isDoor);
        }

        // Pared Derecha (Col 12) -> da a Jaulas y Quirófano
        for (int r = 8; r < _rows; r++)
        {
            bool isDoor = (r == 10 || r == 16); // Puertas a Sala de Jaulas y Quirófano/Boss
            PutWallV(12, r, isDoor);
        }

        // 4. DIVISORIA OESTE: Separa Consultorio 1 de Consultorio 2 (Fila 14, Cols 0..7)
        for (int c = 0; c < 8; c++)
        {
            PutWallH(c, 14, false);
        }

        // 5. DIVISORIA ESTE: Separa Sala de Jaulas de Quirófano (Fila 14, Cols 12..19)
        for (int c = 12; c < _columns; c++)
        {
            PutWallH(c, 14, false);
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Éxito", "¡Plano realista de Clínica Veterinaria generado con éxito!", "¡Genial!");
    }

    // ==========================================
    // 3B. GENERAR SALAS MODULARES CUADRICULADAS
    // ==========================================
    private void GenerateInteriorRooms()
    {
        if (_wallPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Asigná el prefab de pared.", "OK");
            return;
        }

        GameObject parent = GetOrCreateParent("Clinica_SalasInteriores");
        ClearChildren(parent);

        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;
        Quaternion baseRot = _wallPrefab.transform.rotation;

        // Paredes divisorias verticales (a lo largo de Z)
        for (int c = _roomGridStep; c < _columns; c += _roomGridStep)
        {
            float posX = startX + (c * _tileSize) - (_tileSize * 0.5f);
            for (int r = 0; r < _rows; r++)
            {
                float posZ = startZ + (r * _tileSize);
                bool isDoor = (_doorWallPrefab != null) && (Random.value < _doorwayChance);
                GameObject prefab = isDoor ? _doorWallPrefab : _wallPrefab;
                SpawnPiece(prefab, new Vector3(posX, 0f, posZ), Quaternion.Euler(0f, 90f, 0f) * baseRot, parent.transform, $"Pared_Interna_V_{c}_{r}");
            }
        }

        // Paredes divisorias horizontales (a lo largo de X)
        for (int r = _roomGridStep; r < _rows; r += _roomGridStep)
        {
            float posZ = startZ + (r * _tileSize) - (_tileSize * 0.5f);
            for (int c = 0; c < _columns; c++)
            {
                // Evitamos colocar pared exactamente donde ya se cruza con una vertical
                if (c % _roomGridStep == 0) continue;

                float posX = startX + (c * _tileSize);
                bool isDoor = (_doorWallPrefab != null) && (Random.value < _doorwayChance);
                GameObject prefab = isDoor ? _doorWallPrefab : _wallPrefab;
                SpawnPiece(prefab, new Vector3(posX, 0f, posZ), Quaternion.Euler(0f, 0f, 0f) * baseRot, parent.transform, $"Pared_Interna_H_{c}_{r}");
            }
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Éxito", "¡Salas interiores generadas con puertas!", "OK");
    }

    // ==========================================
    // 4. AMOBLAR Y DECORAR LA CLÍNICA
    // ==========================================
    private void AutoFindFurnitureAssets(bool force = false)
    {
        if (force || _receptionDeskPrefab == null)
            _receptionDeskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/recepcion escritorio/Meshy_AI_vet_reception_desk_3d_20261106_texture.fbx");
        if (force || _examTablePrefab == null)
            _examTablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/camilla examinacion/Meshy_AI_vet_examination_table_3d_20261106_texture.fbx");
        if (force || _medicalCabinetPrefab == null)
            _medicalCabinetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/mueble medicinas/Meshy_AI_vet_medical_cabinet_3d_20261106_texture.fbx");
        if (force || _medicalCartPrefab == null)
            _medicalCartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/carrito medico/Meshy_AI_vet_medical_cart_3d_20261106_texture.fbx");
        if (force || _cagePrefab == null)
            _cagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/jaulas low/Meshy_AI_vet_clinic_kennel_60k_1026165409_texture_fbx/Meshy_AI_vet_clinic_kennel_60k_1026165409_texture.fbx");
        if (force || _corruptCatPrefab == null)
            _corruptCatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GatoCorrupto.prefab");
    }

    private void DrawEnemiesTab()
    {
        GUILayout.Label("Poblar Salas con Gatos Corruptos (Dificultad Progresiva)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Genera los gatos corruptos sala por sala con dificultad creciente (sin tocar tus muebles):\n" +
            "• Sala 1 (Recepción): 1 gato inicial (50 HP)\n" +
            "• Sala 2 (Farmacia): 2 gatos (50 HP)\n" +
            "• Sala 3 (Consultorio 1): 3 gatos (50 HP)\n" +
            "• Sala 4 (Consultorio 2): 3 gatos más resistentes (75 HP)\n" +
            "• Sala 5 (Internación / Jaulas): 4 gatos bravos (75 HP)\n" +
            "• Sala 6 (Quirófano): 1 Gato Jefe Gigante (250 HP) + 2 escoltas",
            MessageType.Info);

        EditorGUILayout.Space(5);
        _corruptCatPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Gato Corrupto", _corruptCatPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("🐱 Generar Gatos por Habitación (Dificultad Progresiva)", GUILayout.Height(38)))
        {
            GenerateProgressiveEnemies();
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("💊 Activar Botiquines en los Armarios Médicos de la Escena", GUILayout.Height(32)))
        {
            SetupMedicineCabinetsInScene();
        }
    }

    private void SetupMedicineCabinetsInScene()
    {
        int count = 0;
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.Contains("Armario") && go.GetComponent<MedicineCabinet>() == null && go.transform.parent != null && go.transform.parent.name == "Clinica_Decoracion")
            {
                Undo.AddComponent<MedicineCabinet>(go);
                count++;
            }
        }
        EditorUtility.DisplayDialog("Armarios Listos", $"Se activaron {count} armarios médicos en la clínica para poder sacar botiquines con la tecla E.", "Genial");
    }

    private void GenerateProgressiveEnemies()
    {
        AutoFindFurnitureAssets();
        if (_corruptCatPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró Assets/Prefabs/GatoCorrupto.prefab.", "OK");
            return;
        }

        GameObject parent = GetOrCreateParent("Clinica_Enemigos");
        ClearChildren(parent);

        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;

        Vector3 TilePos(float col, float row)
        {
            return new Vector3(startX + (col * _tileSize), 0.02f, startZ + (row * _tileSize));
        }

        void SpawnCat(float col, float row, float yaw, float hp, float dmg, float speed, float scaleMult, string catName)
        {
            Vector3 pos = TilePos(col, row);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(_corruptCatPrefab, parent.transform);
            obj.name = catName;
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.transform.localScale = Vector3.one * scaleMult;

            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ConfigureStats(hp, dmg, speed, 7.2f);
                EditorUtility.SetDirty(enemy);
            }

            Undo.RegisterCreatedObjectUndo(obj, $"Spawn {catName}");
        }

        // 1. RECEPCIÓN (Nivel 1: 1 gato tranquilo cerca de la puerta al pasillo)
        SpawnCat(7.5f, 5.5f, 200f, 50f, 10f, 3.8f, 1f, "Recepcion_Gato_1");

        // 2. FARMACIA (Nivel 2: 2 gatos cuidando los suministros)
        SpawnCat(14.5f, 2.5f, 270f, 50f, 12f, 4.0f, 1f, "Farmacia_Gato_1");
        SpawnCat(16.8f, 5.2f, 210f, 50f, 12f, 4.0f, 1f, "Farmacia_Gato_2");

        // 3. CONSULTORIO 1 (Nivel 3: 3 gatos)
        SpawnCat(5.0f, 9.5f, 90f, 50f, 12f, 4.1f, 1f, "Consultorio1_Gato_1");
        SpawnCat(2.2f, 10.2f, 110f, 50f, 12f, 4.1f, 1f, "Consultorio1_Gato_2");
        SpawnCat(4.5f, 12.0f, 140f, 50f, 12f, 4.2f, 1f, "Consultorio1_Gato_3");

        // 4. CONSULTORIO 2 (Nivel 4: 3 gatos más duros, 3 dardos c/u)
        SpawnCat(5.2f, 15.5f, 90f, 75f, 14f, 4.2f, 1.1f, "Consultorio2_Gato_1");
        SpawnCat(2.2f, 16.0f, 100f, 75f, 14f, 4.2f, 1.1f, "Consultorio2_Gato_2");
        SpawnCat(3.8f, 18.0f, 150f, 75f, 14f, 4.3f, 1.1f, "Consultorio2_Gato_3");

        // 5. SALA DE JAULAS / INTERNACIÓN (Nivel 5: 4 gatos custodiando las jaulas)
        SpawnCat(14.0f, 10.5f, 270f, 75f, 14f, 4.2f, 1.1f, "Internacion_Gato_1");
        SpawnCat(16.5f, 9.8f, 280f, 75f, 14f, 4.3f, 1.1f, "Internacion_Gato_2");
        SpawnCat(15.5f, 11.8f, 240f, 75f, 15f, 4.3f, 1.1f, "Internacion_Gato_3");
        SpawnCat(17.5f, 11.5f, 250f, 75f, 15f, 4.4f, 1.15f, "Internacion_Gato_4");

        // 6. QUIRÓFANO / SALA DEL JEFE (Nivel 6: Gato Jefe Gigante + 2 escoltas)
        SpawnCat(16.0f, 16.2f, 240f, 250f, 25f, 3.9f, 1.85f, "Quirofano_GatoJefe");
        SpawnCat(13.8f, 15.5f, 260f, 75f, 15f, 4.3f, 1.1f, "Quirofano_Escolta_1");
        SpawnCat(16.8f, 18.0f, 220f, 75f, 15f, 4.3f, 1.1f, "Quirofano_Escolta_2");

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Enemigos Generados", "¡Se poblaron las 6 salas con 16 gatos corruptos en dificultad progresiva (incluyendo el Gato Jefe en el Quirófano)!", "¡A jugar!");
    }

    // Me aseguro de que el FBX de Meshy tenga Scale Factor 100 y Bake Axis Conversion activado, y creo su Material URP
    private Material PrepareFurnitureModel(GameObject modelAsset, string matName)
    {
        if (modelAsset == null) return null;
        string assetPath = AssetDatabase.GetAssetPath(modelAsset);
        if (string.IsNullOrEmpty(assetPath)) return null;

        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        // Desactivo bakeAxisConversion porque en estos modelos de Meshy los invierte de cabeza
        if (importer != null && (importer.globalScale < 90f || importer.bakeAxisConversion))
        {
            importer.globalScale = 100f;
            importer.bakeAxisConversion = false;
            importer.SaveAndReimport();
        }

        string matPath = $"Assets/Materials/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            mat = new Material(urpLit);
            mat.enableInstancing = true;

            string texPath = System.IO.Path.ChangeExtension(assetPath, ".png");
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }

            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    private void GenerateClinicDecoration()
    {
        AutoFindFurnitureAssets();

        Material matDesk = PrepareFurnitureModel(_receptionDeskPrefab, "Mat_Recepcion");
        Material matTable = PrepareFurnitureModel(_examTablePrefab, "Mat_Camilla");
        Material matCabinet = PrepareFurnitureModel(_medicalCabinetPrefab, "Mat_ArmarioMedico");
        Material matCart = PrepareFurnitureModel(_medicalCartPrefab, "Mat_CarritoMedico");
        Material matCage = PrepareFurnitureModel(_cagePrefab, "Mat_Jaula");
        AssetDatabase.SaveAssets();

        GameObject parent = GetOrCreateParent("Clinica_Decoracion");
        ClearChildren(parent);

        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;

        // Convierte coordenada de baldosa (columna, fila) a posición 3D en el mundo
        Vector3 TilePos(float col, float row)
        {
            return new Vector3(startX + (col * _tileSize), 0f, startZ + (row * _tileSize));
        }

        void SpawnProp(GameObject asset, Material mat, float col, float row, float yaw, float targetHeightMeters, string propName, bool isCage = false)
        {
            if (asset == null) return;

            Vector3 pos = TilePos(col, row);
            // Primero lo pongo derecho sin giro Y (yaw = 0) para medir su altura exacta en Y sin que la caja de Bounds se ensanche por girar en diagonal
            Quaternion baseRotOnly = Quaternion.Euler(_propBaseRotation);
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.transform);
            obj.name = propName;
            obj.transform.SetPositionAndRotation(pos, baseRotOnly);
            obj.transform.localScale = Vector3.one;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                if (mat != null)
                {
                    foreach (Renderer r in renderers)
                        r.sharedMaterial = mat;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                // Escalo siempre usando la ALTURA real (bounds.size.y) como referencia fija respecto a la veterinaria y las paredes
                float currentHeight = bounds.size.y;
                if (currentHeight > 0.0001f)
                {
                    float scaleFactor = (targetHeightMeters * _propScaleMultiplier) / currentHeight;
                    obj.transform.localScale = Vector3.one * scaleFactor;
                }

                // Ahora sí le aplico el giro en Y (yaw) de su ubicación en la sala
                obj.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * baseRotOnly;

                // Recalculo bounds para apoyar las patas justo arriba de la baldosa (Y = 0.02)
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                float bottomOffset = obj.transform.position.y - bounds.min.y;
                obj.transform.position = new Vector3(pos.x, 0.02f + bottomOffset, pos.z);

                // Agrego BoxCollider en el objeto que tiene la malla para que el jugador y el NavMesh lo esquiven
                MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.GetComponent<Collider>() == null)
                    mf.gameObject.AddComponent<BoxCollider>();
            }

            if (isCage)
            {
                if (obj.GetComponent<Cage>() == null)
                    obj.AddComponent<Cage>();
            }
            else
            {
                foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
                    t.gameObject.isStatic = true;
            }

            Undo.RegisterCreatedObjectUndo(obj, $"Spawn {propName}");
        }

        // 1. RECEPCIÓN / ADMISIÓN Y TRIAGE (Cols 0..11, Filas 0..7)
        // Dos escritorios de recepción bien centrados en la sala (no pegados al muro sur), box de guardia/triage a la izquierda,
        // armarios de fichas contra las paredes y jaulas de ingreso de pacientes
        SpawnProp(_receptionDeskPrefab, matDesk, 5.2f, 2.2f, 0f, _deskHeight, "Recepcion_Mostrador_1");
        SpawnProp(_receptionDeskPrefab, matDesk, 8.2f, 2.2f, 0f, _deskHeight, "Recepcion_Mostrador_2");
        SpawnProp(_examTablePrefab, matTable, 2.2f, 4.8f, 90f, _tableHeight, "Recepcion_CamillaGuardia");
        SpawnProp(_medicalCartPrefab, matCart, 2.2f, 6.2f, 180f, _cartHeight, "Recepcion_CarritoGuardia");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 0.7f, 6.8f, 180f, _cabinetHeight, "Recepcion_Armario_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 3.8f, 6.8f, 180f, _cabinetHeight, "Recepcion_Armario_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 6.2f, 6.8f, 180f, _cabinetHeight, "Recepcion_Armario_3");
        SpawnProp(_cagePrefab, matCage, 10.8f, 5.8f, 270f, _cageHeight, "Recepcion_JaulaIngreso_1", true);
        SpawnProp(_cagePrefab, matCage, 10.8f, 1.2f, 270f, _cageHeight, "Recepcion_JaulaIngreso_2", true);

        // 1B. PASILLO CENTRAL (Cols 8..11, Filas 8..19)
        // Carritos médicos y armarios de emergencia contra los bordes del pasillo (dejando el centro libre para caminar)
        SpawnProp(_medicalCartPrefab, matCart, 8.6f, 12.2f, 90f, _cartHeight, "Pasillo_Carrito_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 11.3f, 13.0f, 270f, _cabinetHeight, "Pasillo_Armario");
        SpawnProp(_medicalCartPrefab, matCart, 8.6f, 18.2f, 90f, _cartHeight, "Pasillo_Carrito_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 10.0f, 18.8f, 180f, _cabinetHeight, "Pasillo_ArmarioFondo");

        // 2. FARMACIA / DEPÓSITO Y LABORATORIO (Cols 12..19, Filas 0..7)
        // Isla central con góndolas de medicamentos (armarios espalda con espalda), mesa de preparación y jaulas de cuarentena
        SpawnProp(_medicalCabinetPrefab, matCabinet, 13.8f, 6.8f, 180f, _cabinetHeight, "Farmacia_ArmarioNorte_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 17.4f, 6.8f, 180f, _cabinetHeight, "Farmacia_ArmarioNorte_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 18.8f, 5.2f, 270f, _cabinetHeight, "Farmacia_ArmarioEste_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 18.8f, 2.5f, 270f, _cabinetHeight, "Farmacia_ArmarioEste_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 16.5f, 0.7f, 0f, _cabinetHeight, "Farmacia_ArmarioSur_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 14.0f, 0.7f, 0f, _cabinetHeight, "Farmacia_ArmarioSur_2");
        // Isla central de farmacia / preparación
        SpawnProp(_examTablePrefab, matTable, 15.8f, 4.2f, 0f, _tableHeight, "Farmacia_MesaLaboratorio");
        SpawnProp(_medicalCartPrefab, matCart, 14.3f, 4.2f, 90f, _cartHeight, "Farmacia_Carrito_1");
        SpawnProp(_medicalCartPrefab, matCart, 17.2f, 4.2f, 270f, _cartHeight, "Farmacia_Carrito_2");
        SpawnProp(_cagePrefab, matCage, 12.8f, 5.8f, 90f, _cageHeight, "Farmacia_JaulaCuarentena", true);

        // 3. CONSULTORIO 1 (Cols 0..7, Filas 8..13)
        // Dos camillas de atención, carritos, armarios y una jaula
        SpawnProp(_examTablePrefab, matTable, 2.8f, 10.8f, 90f, _tableHeight, "Consultorio1_Camilla_1");
        SpawnProp(_examTablePrefab, matTable, 5.5f, 10.8f, 90f, _tableHeight, "Consultorio1_Camilla_2");
        SpawnProp(_medicalCartPrefab, matCart, 2.8f, 12.5f, 180f, _cartHeight, "Consultorio1_Carrito_1");
        SpawnProp(_medicalCartPrefab, matCart, 5.5f, 12.5f, 180f, _cartHeight, "Consultorio1_Carrito_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 0.8f, 12.8f, 180f, _cabinetHeight, "Consultorio1_Armario_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 0.8f, 8.8f, 0f, _cabinetHeight, "Consultorio1_Armario_2");
        SpawnProp(_cagePrefab, matCage, 4.2f, 8.7f, 0f, _cageHeight, "Consultorio1_Jaula", true);

        // 4. CONSULTORIO 2 (Cols 0..7, Filas 14..19)
        SpawnProp(_examTablePrefab, matTable, 2.8f, 16.8f, 90f, _tableHeight, "Consultorio2_Camilla_1");
        SpawnProp(_examTablePrefab, matTable, 5.5f, 16.8f, 90f, _tableHeight, "Consultorio2_Camilla_2");
        SpawnProp(_medicalCartPrefab, matCart, 4.2f, 18.4f, 180f, _cartHeight, "Consultorio2_Carrito");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 0.8f, 18.7f, 180f, _cabinetHeight, "Consultorio2_Armario_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 6.6f, 18.7f, 180f, _cabinetHeight, "Consultorio2_Armario_2");
        SpawnProp(_cagePrefab, matCage, 1.0f, 15.0f, 90f, _cageHeight, "Consultorio2_Jaula_1", true);
        SpawnProp(_cagePrefab, matCage, 4.0f, 14.8f, 0f, _cageHeight, "Consultorio2_Jaula_2", true);

        // 5. SALA DE JAULAS / INTERNACIÓN (Cols 12..19, Filas 8..13)
        SpawnProp(_cagePrefab, matCage, 14.2f, 12.7f, 180f, _cageHeight, "Internacion_Jaula_1", true);
        SpawnProp(_cagePrefab, matCage, 16.8f, 12.7f, 180f, _cageHeight, "Internacion_Jaula_2", true);
        SpawnProp(_cagePrefab, matCage, 18.6f, 11.2f, 270f, _cageHeight, "Internacion_Jaula_3", true);
        SpawnProp(_cagePrefab, matCage, 18.6f, 9.2f, 270f, _cageHeight, "Internacion_Jaula_4", true);
        SpawnProp(_examTablePrefab, matTable, 15.0f, 9.4f, 0f, _tableHeight, "Internacion_Camilla");
        SpawnProp(_medicalCartPrefab, matCart, 13.0f, 12.5f, 180f, _cartHeight, "Internacion_Carrito");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 13.0f, 8.8f, 0f, _cabinetHeight, "Internacion_Armario");

        // 6. QUIRÓFANO / SALA DEL JEFE (Cols 12..19, Filas 14..19)
        SpawnProp(_examTablePrefab, matTable, 15.8f, 16.8f, 0f, _tableHeight, "Quirofano_MesaQuirurgica");
        SpawnProp(_medicalCartPrefab, matCart, 14.2f, 17.5f, 90f, _cartHeight, "Quirofano_Carrito_1");
        SpawnProp(_medicalCartPrefab, matCart, 17.4f, 17.5f, 270f, _cartHeight, "Quirofano_Carrito_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 14.2f, 18.7f, 180f, _cabinetHeight, "Quirofano_Armario_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 17.2f, 18.7f, 180f, _cabinetHeight, "Quirofano_Armario_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 18.7f, 17.2f, 270f, _cabinetHeight, "Quirofano_Armario_3");
        SpawnProp(_cagePrefab, matCage, 18.5f, 15.0f, 270f, _cageHeight, "Quirofano_Jaula", true);

        // Si el piso tiene NavMeshSurface, lo re-horneo automáticamente para que los gatos esquiven los muebles nuevos
        GameObject piso = GameObject.Find("Clinica_Piso");
        if (piso != null)
        {
            var surface = piso.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (surface != null)
                surface.BuildNavMesh();
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Clínica Amoblada", "¡Se decoraron todas las salas de la clínica (Recepción, Farmacia, Consultorios 1 y 2, Sala de Jaulas y Quirófano) y se actualizó el NavMesh!", "¡Excelente!");
    }

    private GameObject GetOrCreateParent(string name)
    {
        GameObject parent = GameObject.Find(name);
        if (parent == null)
        {
            parent = new GameObject(name);
            parent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(parent, $"Crear {name}");
        }
        return parent;
    }

    private void ClearChildren(GameObject parent)
    {
        while (parent.transform.childCount > 0)
        {
            Undo.DestroyObjectImmediate(parent.transform.GetChild(0).gameObject);
        }
    }

    private void SpawnPiece(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent, string name)
    {
        GameObject obj;
        if (PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.SetPositionAndRotation(pos, rot);
        }
        else
        {
            obj = Instantiate(prefab, pos, rot, parent);
        }
        obj.name = name;
        obj.isStatic = true; // CAMBIO: marco estático para que Unity agrupe los draw calls (Static Batching)
        Undo.RegisterCreatedObjectUndo(obj, $"Spawn {name}");
    }

    // Marca como Static todos los pisos y paredes ya generados y apaga sombras innecesarias del piso
    private void OptimizeSceneClinic()
    {
        string[] groupNames = { "Clinica_Piso", "Clinica_Perimetro", "Clinica_SalasInteriores", "Clinica_Decoracion" };
        int count = 0;

        foreach (string groupName in groupNames)
        {
            GameObject group = GameObject.Find(groupName);
            if (group == null) continue;

            bool isFloor = groupName == "Clinica_Piso";
            group.isStatic = true;

            foreach (Transform child in group.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.isStatic = true;
                if (isFloor)
                {
                    MeshRenderer mr = child.GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                count++;
            }
        }

        EditorUtility.DisplayDialog("Optimización Lista", $"Se optimizaron {count} objetos de la clínica (Static Batching activado + sombras de piso apagadas).", "Genial");
    }
}
