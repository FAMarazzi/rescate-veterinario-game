/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 10/30/2026, 11:27:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/6/2026, 5:28:00 PM
 * @Description: Generador completo de niveles: Pisos en matriz, Paredes perimetrales, Habitaciones interiores con puertas
 *   y Decoración automática de cada sala de la clínica (Recepción, Farmacia, Consultorios, Sala de Jaulas y Quirófano).
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEditor;
using UnityEngine;

public class RoomBuilderWindow : EditorWindow
{
    private enum Tab { Piso, ParedesPerimetro, HabitacionesInteriores, Decoracion }
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
    private float _propScaleMultiplier = 1f;

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

        _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, new string[] { "1. Piso", "2. Perímetro", "3. Salas", "4. Decoración" });
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
        _propScaleMultiplier = EditorGUILayout.Slider("Escala de Muebles", _propScaleMultiplier, 0.5f, 2f);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("🔍 Auto-Detectar los 5 Modelos en Assets/Models", GUILayout.Height(26)))
        {
            AutoFindFurnitureAssets(true);
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("🛋️ Amoblar Toda la Clínica Automáticamente", GUILayout.Height(38)))
        {
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
    }

    // Me aseguro de que el FBX de Meshy tenga Scale Factor 100 y Bake Axis Conversion activado, y creo su Material URP
    private Material PrepareFurnitureModel(GameObject modelAsset, string matName)
    {
        if (modelAsset == null) return null;
        string assetPath = AssetDatabase.GetAssetPath(modelAsset);
        if (string.IsNullOrEmpty(assetPath)) return null;

        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer != null && (importer.globalScale < 90f || !importer.bakeAxisConversion))
        {
            importer.globalScale = 100f;
            importer.bakeAxisConversion = true;
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

        void SpawnProp(GameObject asset, Material mat, float col, float row, float yaw, float targetSizeMeters, string propName, bool isCage = false)
        {
            if (asset == null) return;

            Vector3 pos = TilePos(col, row);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.transform);
            obj.name = propName;
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.transform.localScale = Vector3.one;

            // Le pongo su material a todos los renderers hijos y calculo el tamaño total para escalarlo prolijo y apoyarlo al piso
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

                float maxHorizontal = Mathf.Max(bounds.size.x, bounds.size.z, bounds.size.y);
                if (maxHorizontal > 0.0001f)
                {
                    float scaleFactor = (targetSizeMeters * _propScaleMultiplier) / maxHorizontal;
                    obj.transform.localScale = Vector3.one * scaleFactor;
                }

                // Recalculo bounds después de escalar para apoyar la base justo en Y = 0.02 (arriba de la baldosa)
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

        // 1. RECEPCIÓN / ENTRADA (Cols 0..11, Filas 0..7)
        // Mostrador principal mirando a la entrada Sur y armario de suministros contra la pared
        SpawnProp(_receptionDeskPrefab, matDesk, 5.5f, 4.5f, 180f, 2.5f, "Recepcion_Mostrador");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 1.2f, 6.6f, 180f, 1.8f, "Recepcion_Armario");
        SpawnProp(_medicalCartPrefab, matCart, 10.2f, 6.4f, 270f, 1.15f, "Recepcion_Carrito");

        // 2. FARMACIA / SUMINISTROS (Cols 12..19, Filas 0..7)
        // Varios armarios médicos contra las paredes y carritos de medicamentos
        SpawnProp(_medicalCabinetPrefab, matCabinet, 14.0f, 6.7f, 180f, 1.85f, "Farmacia_Armario_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 17.5f, 6.7f, 180f, 1.85f, "Farmacia_Armario_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 18.6f, 3.5f, 270f, 1.85f, "Farmacia_Armario_3");
        SpawnProp(_medicalCartPrefab, matCart, 15.5f, 2.5f, 45f, 1.15f, "Farmacia_Carrito_1");
        SpawnProp(_medicalCartPrefab, matCart, 13.5f, 1.5f, 0f, 1.15f, "Farmacia_Carrito_2");

        // 3. CONSULTORIO 1 (Cols 0..7, Filas 8..13)
        // Camilla en el centro, carrito al lado y armario médico contra la pared
        SpawnProp(_examTablePrefab, matTable, 3.5f, 11.0f, 90f, 1.9f, "Consultorio1_Camilla");
        SpawnProp(_medicalCartPrefab, matCart, 3.5f, 12.6f, 180f, 1.1f, "Consultorio1_Carrito");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 1.0f, 12.8f, 180f, 1.8f, "Consultorio1_Armario");

        // 4. CONSULTORIO 2 (Cols 0..7, Filas 14..19)
        // Camilla de revisación, carrito de instrumental, armario y una jaula de observación
        SpawnProp(_examTablePrefab, matTable, 3.5f, 16.8f, 90f, 1.9f, "Consultorio2_Camilla");
        SpawnProp(_medicalCartPrefab, matCart, 5.2f, 18.2f, 180f, 1.1f, "Consultorio2_Carrito");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 1.0f, 18.6f, 180f, 1.8f, "Consultorio2_Armario");
        SpawnProp(_cagePrefab, matCage, 1.2f, 15.2f, 90f, 1.5f, "Consultorio2_Jaula", true);

        // 5. SALA DE JAULAS / INTERNACIÓN (Cols 12..19, Filas 8..13)
        // Filas de jaulas (Kennels) para rescatar gatitos y una camilla de internación
        SpawnProp(_cagePrefab, matCage, 14.5f, 12.7f, 180f, 1.55f, "Internacion_Jaula_1", true);
        SpawnProp(_cagePrefab, matCage, 17.2f, 12.7f, 180f, 1.55f, "Internacion_Jaula_2", true);
        SpawnProp(_cagePrefab, matCage, 18.5f, 10.2f, 270f, 1.55f, "Internacion_Jaula_3", true);
        SpawnProp(_examTablePrefab, matTable, 15.0f, 9.4f, 0f, 1.8f, "Internacion_Camilla");
        SpawnProp(_medicalCartPrefab, matCart, 13.2f, 12.5f, 180f, 1.1f, "Internacion_Carrito");

        // 6. QUIRÓFANO / SALA DEL JEFE (Cols 12..19, Filas 14..19)
        // Mesa quirúrgica central, dos carritos de cirugía, armarios al fondo y jaula especial
        SpawnProp(_examTablePrefab, matTable, 15.8f, 16.8f, 0f, 2.1f, "Quirofano_MesaQuirurgica");
        SpawnProp(_medicalCartPrefab, matCart, 14.2f, 17.5f, 90f, 1.15f, "Quirofano_Carrito_1");
        SpawnProp(_medicalCartPrefab, matCart, 17.4f, 17.5f, 270f, 1.15f, "Quirofano_Carrito_2");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 14.5f, 18.7f, 180f, 1.85f, "Quirofano_Armario_1");
        SpawnProp(_medicalCabinetPrefab, matCabinet, 17.2f, 18.7f, 180f, 1.85f, "Quirofano_Armario_2");
        SpawnProp(_cagePrefab, matCage, 18.5f, 15.2f, 270f, 1.6f, "Quirofano_Jaula", true);

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
