/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 10/30/2026, 10:46:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/30/2026, 10:45:49 AM
 * @Description: Herramienta de Editor para generar matrices de piso/paredes en un click.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEditor;
using UnityEngine;

public class RoomBuilderWindow : EditorWindow
{
    private GameObject _tilePrefab;
    private int _columns = 5;
    private int _rows = 5;
    private float _tileSize = 1.885f; // Apenas más chica que 1.903 para que se solapen milimétricamente y no quede luz
    private bool _randomRotation = true; // Rota al azar 0°, 90°, 180° o 270° para romper la monotonía visual
    private string _parentName = "Sala1_Piso";

    [MenuItem("Tools/Generador de Pisos")]
    public static void ShowWindow()
    {
        GetWindow<RoomBuilderWindow>("Generador de Pisos");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generador de Matriz de Pisos", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Prefab de Baldosa", _tilePrefab, typeof(GameObject), true);
        _columns = EditorGUILayout.IntSlider("Columnas (X)", _columns, 1, 20);
        _rows = EditorGUILayout.IntSlider("Filas (Z)", _rows, 1, 20);
        _tileSize = EditorGUILayout.FloatField("Distancia entre Baldosas (m)", _tileSize);
        _randomRotation = EditorGUILayout.Toggle("Rotación Aleatoria (90°)", _randomRotation);
        _parentName = EditorGUILayout.TextField("Nombre del Padre", _parentName);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Generar Matriz de Piso", GUILayout.Height(35)))
        {
            GenerateGrid();
        }
    }

    private void GenerateGrid()
    {
        if (_tilePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Arrastrá la baldosa o su prefab al campo 'Prefab de Baldosa'.", "OK");
            return;
        }

        // Si ya existe la sala anterior, preguntamos o limpiamos para no acumular pisos viejos encima
        GameObject existingParent = GameObject.Find(_parentName);
        if (existingParent != null)
        {
            if (EditorUtility.DisplayDialog("Aviso", $"Ya existe un objeto llamado '{_parentName}'. ¿Querés reemplazarlo?", "Reemplazar", "Crear Nuevo"))
            {
                Undo.DestroyObjectImmediate(existingParent);
            }
        }

        GameObject parent = new GameObject(_parentName);
        parent.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(parent, "Crear Sala Piso");

        // Centramos la matriz respecto al origen
        float startX = -((_columns - 1) * _tileSize) * 0.5f;
        float startZ = -((_rows - 1) * _tileSize) * 0.5f;

        Quaternion baseRotation = _tilePrefab.transform.rotation;

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                Vector3 pos = new Vector3(startX + (c * _tileSize), 0f, startZ + (r * _tileSize));
                
                // Rotación aleatoria en pasos de 90° sobre el eje Y del mundo (para no romper la inclinación horizontal)
                Quaternion finalRot = baseRotation;
                if (_randomRotation)
                {
                    int quarterTurns = Random.Range(0, 4); // 0, 1, 2 o 3
                    finalRot = Quaternion.Euler(0f, quarterTurns * 90f, 0f) * baseRotation;
                }

                GameObject tile;
                if (PrefabUtility.IsPartOfPrefabAsset(_tilePrefab))
                {
                    tile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab, parent.transform);
                    tile.transform.SetPositionAndRotation(pos, finalRot);
                }
                else
                {
                    tile = Instantiate(_tilePrefab, pos, finalRot, parent.transform);
                }

                tile.name = $"Piso_{c}_{r}";
                Undo.RegisterCreatedObjectUndo(tile, "Generar Baldosa");
            }
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("Éxito", $"¡Matriz de {_columns}x{_rows} generada con éxito!", "Genial");
    }
}

