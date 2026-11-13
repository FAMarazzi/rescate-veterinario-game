/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/6/2026, 8:27:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 7:16:00 PM
 * @Description: Aliado animal (Dibu y Cuti). Hereda de Entity, sigue al jugador manteniendo una distancia cómoda
 *   y ataca a los gatos corruptos. Nunca muere: si los enemigos le bajan toda la vida se vuelve corrupto
 *   temporalmente, y la veterinaria puede volverlo a la normalidad disparándole dardos sedantes.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Ally : Entity
{
    public static readonly List<Ally> ActiveAllies = new List<Ally>();

    [Header("Seguimiento al Jugador")]
    [SerializeField] private float _followSpeed = 3.5f;    // camina tranqui cuando me sigue así no parece que me quiere atacar
    [SerializeField] private float _combatSpeed = 6f;      // corre rápido recién cuando ve a un corrupto para ir a pegarle
    [SerializeField] private float _followDistance = 3.2f; // se frena lejos mío para darme espacio y no encimarse

    [Header("Combate contra Corruptos")]
    [SerializeField] private float _enemyDetectRange = 9f;
    [SerializeField] private float _attackRange = 1.6f;
    [SerializeField] private float _attackDamage = 15f;
    [SerializeField] private float _attackCooldown = 0.9f;

    [Header("Estado Corrupto Temporal (Nunca Muere)")]
    [SerializeField] private Color _corruptTint = new Color(0.68f, 0.22f, 0.92f);
    [SerializeField] private float _corruptMoveSpeed = 3.8f;
    [SerializeField] private float _corruptAttackDamage = 8f;

    [Header("Indicador Visual (Aro en el Piso)")]
    [SerializeField] private Color _allyRingColor = new Color(0.2f, 0.95f, 0.35f);
    [SerializeField] private Color _corruptRingColor = new Color(0.85f, 0.18f, 0.95f);
    [SerializeField] private float _ringRadius = 0.65f;
    [SerializeField] private float _ringWidth = 0.06f;

    [Header("Escala al Liberarse")]
    [Tooltip("Si es mayor a 0, fuerza esta escala uniforme al salir de la jaula. En 0 conserva su tamaño real en escena/prefab.")]
    [SerializeField] private float _releaseScale = 0f;

    public bool IsCorrupted => _isCorrupted;

    /// <summary>
    /// Aplica una escala uniforme al liberarse de la jaula solo si se configuró un valor mayor a 0
    /// en la jaula o en este aliado; si ambos están en 0, conserva su escala real en el mundo.
    /// </summary>
    public void ApplyReleaseScale(float cageOverrideScale = 0f)
    {
        float targetScale = cageOverrideScale > 0f ? cageOverrideScale : _releaseScale;
        if (targetScale > 0f)
            transform.localScale = Vector3.one * targetScale;
    }

    /// <summary>
    /// Mide los bounds reales del modelo 3D y lo apoya exactamente al ras del piso (transform.position.y),
    /// sin importar si el pivote del FBX está en las patas (Dibu) o en el centro del cuerpo (Cuti).
    /// </summary>
    public void SnapModelToFloor()
    {
        Transform model = GetModelTransform();
        if (model == null) return;

        Renderer[] rends = model.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        float minY = float.MaxValue;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null || !rends[i].enabled) continue;
            if (rends[i] is LineRenderer) continue;
            if (rends[i].gameObject.name.StartsWith("HB_")) continue;

            if (rends[i].bounds.min.y < minY)
            {
                minY = rends[i].bounds.min.y;
                found = true;
            }
        }

        if (found)
        {
            float deltaY = transform.position.y - minY;
            model.position += Vector3.up * deltaY;
        }

        _baseModelLocalPos = model.localPosition;
        _hasBaseModelPos = true;
    }

    private Transform GetModelTransform()
    {
        if (_modelTransform != null) return _modelTransform;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name != "HealthBar_Root" && child.name != "AroAliado")
            {
                _modelTransform = child;
                break;
            }
        }
        return _modelTransform;
    }

    private Transform _modelTransform;
    private Vector3 _baseModelLocalPos;
    private bool _hasBaseModelPos;

    private bool _isCorrupted = false;
    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private PlayerController _playerController;
    private Enemy _currentTarget;
    private LineRenderer _ringRenderer;
    private WorldHealthBar _healthBar;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private string _playerTag = "Player";
    private float _attackTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        SetupValidAnimator();
        if (_agent != null)
            _agent.speed = _followSpeed;

        // Me aseguro de que tenga un CapsuleCollider alto por si pasa a estado corrupto y necesito pegarle con dardos,
        // pero arranca apagado mientras es aliado para que nunca me trabe en las puertas
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap == null && GetComponent<Collider>() == null)
        {
            cap = gameObject.AddComponent<CapsuleCollider>();
        }
        if (cap != null)
        {
            cap.height = 1.6f;
            cap.center = new Vector3(0f, 0.8f, 0f);
            cap.radius = 0.4f;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        CacheRenderers();
        CreateGreenRing();
    }

    private void OnEnable()
    {
        if (!ActiveAllies.Contains(this))
            ActiveAllies.Add(this);

        if (_ringRenderer != null)
        {
            _ringRenderer.enabled = true;
            UpdateGreenRingPositions();
        }
    }

    private void OnDisable()
    {
        ActiveAllies.Remove(this);

        if (_ringRenderer != null)
            _ringRenderer.enabled = false;
    }

    private void Start()
    {
        FindPlayer();

        _healthBar = GetComponent<WorldHealthBar>();
        if (_healthBar == null)
        {
            _healthBar = gameObject.AddComponent<WorldHealthBar>();
            _healthBar.AttachTo(this, true, false);
        }
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material mat = _renderers[i].material;
            if (mat.HasProperty("_BaseColor"))
                _originalColors[i] = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                _originalColors[i] = mat.color;
            else
                _originalColors[i] = Color.white;
        }
    }

    private void ApplyFurTint(bool corrupted)
    {
        if (_renderers == null) return;
        Color emissionGlow = _corruptTint * 0.55f;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null || _renderers[i] == _ringRenderer) continue;
            Material mat = _renderers[i].material;
            Color targetCol = corrupted ? _corruptTint : _originalColors[i];
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", targetCol);
            if (mat.HasProperty("_Color"))
                mat.color = targetCol;

            if (mat.HasProperty("_EmissionColor"))
            {
                if (corrupted)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionGlow);
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private void FindPlayer()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
        if (_playerController != null)
        {
            _player = _playerController.transform;
            if (!string.IsNullOrEmpty(_playerController.gameObject.tag))
                _playerTag = _playerController.gameObject.tag;

            if (!_isCorrupted)
            {
                gameObject.tag = _playerTag;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (_player == null) FindPlayer();

        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;

        if (_isCorrupted)
        {
            // Si está corrompido, persigue a la veterinaria hasta que ella lo cure con dardos sedantes
            UpdateCorruptedBehavior();
        }
        else
        {
            // Estado normal de aliado: busca enemigos corruptos o sigue a la veterinaria
            _currentTarget = FindClosestEnemy();

            if (_currentTarget != null)
                FightEnemy();
            else if (_player != null)
                FollowPlayer();
        }

        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            float speed = (_agent != null && _agent.enabled && _agent.isOnNavMesh && !_agent.isStopped)
                ? _agent.velocity.magnitude
                : 0f;
            _animator.SetFloat(SpeedHash, speed);
        }
        else
        {
            Transform model = GetModelTransform();
            if (model != null)
            {
                if (!_hasBaseModelPos)
                {
                    _baseModelLocalPos = model.localPosition;
                    _hasBaseModelPos = true;
                }
                bool moving = _agent != null && _agent.enabled && _agent.isOnNavMesh && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.05f;
                float yHop = moving ? Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.08f : 0f;
                model.localPosition = _baseModelLocalPos + transform.InverseTransformVector(Vector3.up * yHop);
            }
        }
    }

    private void SetupValidAnimator()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        _animator = null;
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i].runtimeAnimatorController != null)
            {
                _animator = animators[i];
                _animator.applyRootMotion = false;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.enabled = true;
            }
            else
            {
                animators[i].enabled = false;
            }
        }
    }

    private void UpdateCorruptedBehavior()
    {
        if (_player == null || (_playerController != null && _playerController.IsDead))
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;
            return;
        }

        if (_agent != null)
            _agent.speed = _corruptMoveSpeed;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _attackRange)
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;

            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);

            if (_attackTimer <= 0f && _playerController != null)
            {
                _playerController.TakeDamage(_corruptAttackDamage);
                _attackTimer = _attackCooldown * 1.25f;
            }
        }
        else if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }
    }

    private void FightEnemy()
    {
        if (_agent != null) _agent.speed = _combatSpeed;

        float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);

        if (dist <= _attackRange)
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;

            Vector3 dir = _currentTarget.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);

            if (_attackTimer <= 0f)
            {
                _currentTarget.TakeDamage(_attackDamage);
                _attackTimer = _attackCooldown;
            }
        }
        else if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_currentTarget.transform.position);
        }
    }

    private void FollowPlayer()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        _agent.speed = _followSpeed;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist < 1.3f)
        {
            Vector3 stepAside = transform.position + (transform.position - _player.position).normalized * 1.5f;
            _agent.isStopped = false;
            _agent.SetDestination(stepAside);
        }
        else if (dist > _followDistance)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }
        else
        {
            _agent.isStopped = true;
        }
    }

    private void CreateGreenRing()
    {
        GameObject ringObj = new GameObject("AroAliado");
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = Vector3.zero;

        _ringRenderer = ringObj.AddComponent<LineRenderer>();
        _ringRenderer.useWorldSpace = true;
        _ringRenderer.loop = true;
        _ringRenderer.startWidth = _ringWidth;
        _ringRenderer.endWidth = _ringWidth;
        _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _ringRenderer.receiveShadows = false;
        _ringRenderer.enabled = enabled;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null)
            _ringRenderer.material = new Material(sh);

        _ringRenderer.startColor = _allyRingColor;
        _ringRenderer.endColor = _allyRingColor;

        _ringRenderer.positionCount = 24;
        UpdateGreenRingPositions();
    }

    private void LateUpdate()
    {
        if (_ringRenderer != null && _ringRenderer.enabled)
            UpdateGreenRingPositions();
    }

    private void UpdateGreenRingPositions()
    {
        if (_ringRenderer == null) return;
        int segments = _ringRenderer.positionCount;
        Vector3 center = new Vector3(transform.position.x, transform.position.y + 0.05f, transform.position.z);
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            _ringRenderer.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * _ringRadius, 0f, Mathf.Sin(angle) * _ringRadius));
        }
    }

    private Enemy FindClosestEnemy()
    {
        Enemy closest = null;
        float minDist = _enemyDetectRange;

        for (int i = 0; i < Enemy.ActiveEnemies.Count; i++)
        {
            Enemy e = Enemy.ActiveEnemies[i];
            if (e == null || e.IsDead || !e.enabled) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = e;
            }
        }

        return closest;
    }

    // Dibu y Cuti NUNCA mueren:
    // - Si eran aliados y los enemigos les bajan la vida a 0 -> se vuelven corruptos (violetas).
    // - Si estaban corruptos y la veterinaria les baja la barra con dardos -> vuelven a la normalidad (verdes).
    protected override void Die()
    {
        if (!_isCorrupted)
        {
            BecomeCorrupted();
        }
        else
        {
            CureBackToAlly();
        }
    }

    private void BecomeCorrupted()
    {
        _isCorrupted = true;
        ResetHealth();

        // Le cambio el tag para que mis dardos tranquilizantes ahora le puedan pegar y curarlo
        gameObject.tag = "Untagged";

        // Prendo su Collider para recibir los dardos pero ignoro el choque físico contra el jugador para no trabarlo
        Collider myCol = GetComponent<Collider>();
        if (myCol != null)
        {
            myCol.enabled = true;
            myCol.isTrigger = false;
            if (_playerController != null)
            {
                Collider playerCol = _playerController.GetComponent<Collider>();
                if (playerCol != null)
                    Physics.IgnoreCollision(myCol, playerCol, true);
            }
        }

        ApplyFurTint(true);

        if (_ringRenderer != null)
        {
            _ringRenderer.startColor = _corruptRingColor;
            _ringRenderer.endColor = _corruptRingColor;
        }

        if (_healthBar != null)
            _healthBar.SetAllyCorruptedVisual(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowNotification(
                $"¡{gameObject.name} fue corrompido! ¡Disparale dardos sedantes para volverlo a la normalidad!",
                new Color(0.95f, 0.35f, 1f));
        }
    }

    private void CureBackToAlly()
    {
        _isCorrupted = false;
        ResetHealth();

        // Vuelve al bando del jugador y apago su Collider para que nunca estorbe en las puertas
        gameObject.tag = _playerTag;
        Collider myCol = GetComponent<Collider>();
        if (myCol != null)
            myCol.enabled = false;

        ApplyFurTint(false);

        if (_ringRenderer != null)
        {
            _ringRenderer.startColor = _allyRingColor;
            _ringRenderer.endColor = _allyRingColor;
        }

        if (_healthBar != null)
            _healthBar.SetAllyCorruptedVisual(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCatCured();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowNotification(
                $"¡Curaste a {gameObject.name}! Volvió a la normalidad y pelea a tu lado.",
                new Color(0.35f, 1f, 0.55f));
        }
    }
}
