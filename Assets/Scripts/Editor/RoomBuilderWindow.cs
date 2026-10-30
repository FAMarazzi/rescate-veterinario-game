/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 10/30/2026, 11:27:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/30/2026, 11:26:35 AM
 * @Description: Generador completo de niveles: Pisos en matriz, Paredes perimetrales y Habitaciones interiores con puertas.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEditor;
using UnityEngine;

public class RoomBuilderWindow : EditorWindow
{
    private enum Tab { Piso, ParedesPerimetro, HabitacionesInteriores }
    private Tab _currentTab = Tab.Piso;

    [Header("Configuración General")]
    private int _columns = 20;
    private int _rows = 20;
    private float _tileSize = 1.885f;

    [Header("Prefabs")]
    private GameObject _tilePrefab;
    private GameObject _wallPrefab;
    private GameObject _doorWallPrefab;

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

    private void OnGUI()
    {
        GUILayout.Label("Generador de Clínica Veterinaria", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, new string[] { "1. Piso", "2. Paredes Exteriores", "3. Salas Interiores" });
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
        Undo.RegisterCreatedObjectUndo(obj, $"Spawn {name}");
    }
}
