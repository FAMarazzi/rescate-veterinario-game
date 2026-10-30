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
        
        EditorGUILayout.Space(5);
        _roomGridStep = EditorGUILayout.IntSlider("Tamaño de Salas (baldosas)", _roomGridStep, 3, 10);
        _doorwayChance = EditorGUILayout.Slider("Probabilidad de Puertas", _doorwayChance, 0.1f, 0.8f);

        EditorGUILayout.HelpBox($"Dividirá el mapa de {_columns}x{_rows} en habitaciones de aprox {_roomGridStep}x{_roomGridStep} baldosas con puertas aleatorias entre ellas.", MessageType.Info);

        EditorGUILayout.Space(15);
        if (GUILayout.Button("Generar Salas Interiores", GUILayout.Height(35)))
        {
            GenerateInteriorRooms();
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
    // 3. GENERAR SALAS INTERIORES
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
