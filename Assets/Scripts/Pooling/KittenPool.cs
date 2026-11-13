/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/12/2026, 8:40:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/12/2026, 1:46:31 PM
 * @Description: Object Pool para los gatitos rescatados de las jaulas comunes (Queue<RescuedKitten>).
 *   Evita hacer Instantiate y Destroy en tiempo de ejecución cuando el jugador abre varias jaulas seguidas.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KittenPool : MonoBehaviour
{
    public static KittenPool Instance { get; private set; }
    private static bool _sceneHookRegistered = false;

    [SerializeField] private GameObject _kittenTemplatePrefab;
    [SerializeField] private int _initialSize = 5;

    private readonly Queue<RescuedKitten> _available = new Queue<RescuedKitten>();
    private GameObject _runtimeTemplate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (!_sceneHookRegistered)
        {
            _sceneHookRegistered = true;
            SceneManager.sceneLoaded += (scene, mode) => EnsureInstance();
        }
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (Instance == null && FindFirstObjectByType<Cage>() != null)
        {
            GameObject go = new GameObject("KittenPool");
            go.AddComponent<KittenPool>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        PrepareTemplate();
        if (_runtimeTemplate != null)
        {
            for (int i = 0; i < _initialSize; i++)
            {
                CreateNewKitten();
            }
        }
    }

    private void PrepareTemplate()
    {
        GameObject source = _kittenTemplatePrefab;
        if (source == null)
        {
            Enemy anyCat = FindFirstObjectByType<Enemy>();
            if (anyCat != null)
                source = anyCat.gameObject;
        }

        if (source == null) return;

        // Creo un molde limpio y desactivado una sola vez al inicio de la escena
        _runtimeTemplate = Instantiate(source, transform);
        _runtimeTemplate.name = "KittenPool_Template";
        _runtimeTemplate.SetActive(false);
        _runtimeTemplate.transform.localScale = Vector3.one * 0.85f;

        Enemy enemyComp = _runtimeTemplate.GetComponent<Enemy>();
        if (enemyComp != null) DestroyImmediate(enemyComp);

        Ally allyComp = _runtimeTemplate.GetComponent<Ally>();
        if (allyComp != null) DestroyImmediate(allyComp);

        WorldHealthBar hb = _runtimeTemplate.GetComponent<WorldHealthBar>();
        if (hb != null) DestroyImmediate(hb);

        Collider col = _runtimeTemplate.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = _runtimeTemplate.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (Renderer r in _runtimeTemplate.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            Material mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color")) mat.color = Color.white;
        }

        if (_runtimeTemplate.GetComponent<RescuedKitten>() == null)
            _runtimeTemplate.AddComponent<RescuedKitten>();
    }

    private RescuedKitten CreateNewKitten()
    {
        if (_runtimeTemplate == null) PrepareTemplate();
        if (_runtimeTemplate == null) return null;

        GameObject obj = Instantiate(_runtimeTemplate, transform);
        obj.name = "GatitoRescatado_Pool";
        obj.SetActive(false);

        RescuedKitten rk = obj.GetComponent<RescuedKitten>();
        if (rk == null) rk = obj.AddComponent<RescuedKitten>();

        _available.Enqueue(rk);
        return rk;
    }

    public RescuedKitten Get(Vector3 spawnPos, Vector3 exitPos)
    {
        if (_available.Count == 0)
        {
            CreateNewKitten();
        }

        if (_available.Count == 0) return null;

        RescuedKitten rk = _available.Dequeue();
        rk.transform.position = spawnPos;
        rk.transform.rotation = Quaternion.identity;
        rk.gameObject.SetActive(true);
        rk.RunToExit(exitPos);
        return rk;
    }

    public void Return(RescuedKitten rk)
    {
        if (rk == null || !rk.gameObject.activeSelf) return;

        rk.gameObject.SetActive(false);
        rk.transform.SetParent(transform);
        _available.Enqueue(rk);
    }
}

