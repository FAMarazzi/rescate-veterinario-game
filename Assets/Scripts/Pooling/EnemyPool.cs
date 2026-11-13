/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/12/2026, 8:48:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/12/2026, 8:48:00 AM
 * @Description: Pool y gestor de activación de Gatos Corruptos por cercanía a cada habitación (EnemyPool).
 *   Mantiene desactivados en el pool a los gatos de las salas lejanas para no gastar CPU/GPU/Animator/NavMesh,
 *   los activa (Get) cuando el jugador se acerca a la sala, y recicla (Return) los gatos ya sedados cuando
 *   el jugador abandona la habitación limpia.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }
    private static bool _sceneHookRegistered = false;

    [SerializeField] private float _activationDistance = 16.5f; // Distancia a la que se activan los gatos de una sala
    [SerializeField] private float _recycleSleptDistance = 15.0f; // Distancia a la que un gato ya dormido vuelve al pool

    private class PendingEnemySlot
    {
        public Enemy EnemyInstance;
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
        public bool HasSpawned;
    }

    private readonly Queue<Enemy> _pooledSleepingCats = new Queue<Enemy>();
    private readonly List<PendingEnemySlot> _roomSlots = new List<PendingEnemySlot>();
    private readonly List<Enemy> _sleepingActiveCats = new List<Enemy>();

    private Transform _player;
    private float _checkTimer;

    public int TotalEnemyCount { get; private set; }

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
        if (Instance == null && FindFirstObjectByType<Enemy>() != null)
        {
            GameObject go = new GameObject("EnemyPool");
            go.AddComponent<EnemyPool>();
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

        RegisterSceneEnemiesIntoPool();
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            _player = pc.transform;

        // Activo inmediatamente los gatos de las salas cercanas al punto de inicio (Recepción)
        EvaluateRoomsProximity();
    }

    private void RegisterSceneEnemiesIntoPool()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TotalEnemyCount = allEnemies.Length;

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        Vector3 playerPos = pc != null ? pc.transform.position : Vector3.zero;

        foreach (Enemy e in allEnemies)
        {
            if (e == null) continue;

            e.OnDied += HandleEnemySlept;

            PendingEnemySlot slot = new PendingEnemySlot
            {
                EnemyInstance = e,
                SpawnPosition = e.transform.position,
                SpawnRotation = e.transform.rotation,
                HasSpawned = false
            };

            // Si está cerca del inicio (ej. Recepción) ya arranca activo; si está en salas lejanas queda en espera en el pool
            float dist = Vector3.Distance(playerPos, slot.SpawnPosition);
            if (dist <= _activationDistance)
            {
                slot.HasSpawned = true;
                e.gameObject.SetActive(true);
            }
            else
            {
                e.gameObject.SetActive(false);
            }

            _roomSlots.Add(slot);
        }
    }

    private void Update()
    {
        _checkTimer -= Time.deltaTime;
        if (_checkTimer > 0f) return;
        _checkTimer = 0.25f;

        if (_player == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) _player = pc.transform;
            if (_player == null) return;
        }

        EvaluateRoomsProximity();
    }

    private void EvaluateRoomsProximity()
    {
        if (_player == null) return;

        Vector3 pPos = _player.position;
        float actSqr = _activationDistance * _activationDistance;
        float recSqr = _recycleSleptDistance * _recycleSleptDistance;

        // 1. Activo (Get del pool) los gatos de la habitación a la que el jugador se está acercando
        for (int i = 0; i < _roomSlots.Count; i++)
        {
            PendingEnemySlot slot = _roomSlots[i];
            if (slot.HasSpawned || slot.EnemyInstance == null) continue;

            if ((slot.SpawnPosition - pPos).sqrMagnitude <= actSqr)
            {
                slot.HasSpawned = true;
                ActivateFromPool(slot);
            }
        }

        // 2. Devuelvo al pool (Return) los gatos que ya fueron sedados cuando el jugador se va de esa habitación
        for (int i = _sleepingActiveCats.Count - 1; i >= 0; i--)
        {
            Enemy sleptCat = _sleepingActiveCats[i];
            if (sleptCat == null)
            {
                _sleepingActiveCats.RemoveAt(i);
                continue;
            }

            if ((sleptCat.transform.position - pPos).sqrMagnitude > recSqr)
            {
                ReturnToPool(sleptCat);
                _sleepingActiveCats.RemoveAt(i);
            }
        }
    }

    private void ActivateFromPool(PendingEnemySlot slot)
    {
        Enemy e = slot.EnemyInstance;
        e.transform.SetPositionAndRotation(slot.SpawnPosition, slot.SpawnRotation);
        e.gameObject.SetActive(true);
    }

    private void HandleEnemySlept(Entity entity)
    {
        if (entity is Enemy enemy && !_sleepingActiveCats.Contains(enemy))
        {
            _sleepingActiveCats.Add(enemy);
        }
    }

    public void ReturnToPool(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeSelf) return;

        enemy.gameObject.SetActive(false);
        _pooledSleepingCats.Enqueue(enemy);
    }
}

