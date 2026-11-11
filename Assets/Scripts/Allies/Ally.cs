/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/6/2026, 8:27:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/6/2026, 8:27:00 AM
 * @Description: Aliado animal (Dibu, Cuti o gato/perro curado). Hereda de Entity,
 *   sigue al jugador manteniendo una distancia cómoda y si ve un enemigo corrupto cerca va y lo ataca.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Ally : Entity
{
    [Header("Seguimiento al Jugador")]
    [SerializeField] private float _followSpeed = 3.5f;    // camina tranqui cuando me sigue así no parece que me quiere atacar
    [SerializeField] private float _combatSpeed = 6f;      // corre rápido recién cuando ve a un corrupto para ir a pegarle
    [SerializeField] private float _followDistance = 3.2f; // se frena lejos mío para darme espacio y no encimarse

    [Header("Combate contra Corruptos")]
    [SerializeField] private float _enemyDetectRange = 9f;
    [SerializeField] private float _attackRange = 1.6f;
    [SerializeField] private float _attackDamage = 15f;
    [SerializeField] private float _attackCooldown = 0.9f;

    [Header("Indicador Visual (Aro Verde)")]
    [SerializeField] private Color _allyRingColor = new Color(0.2f, 0.95f, 0.35f);
    [SerializeField] private float _ringRadius = 0.65f;
    [SerializeField] private float _ringWidth = 0.06f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private Enemy _currentTarget;
    private float _attackTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            _animator.applyRootMotion = false;
        if (_agent != null)
            _agent.speed = _followSpeed;

        // Le apago el Collider al aliado para que nunca me trabe en las puertas ni en los pasillos
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Si viene de ser un enemigo, le congelo la física para que no me empuje al caminar al lado mío
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        CreateGreenRing();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            _player = pc.transform;
            // Le pongo el mismo tag que el jugador para que los dardos del jugador no lo lastimen
            if (!string.IsNullOrEmpty(pc.gameObject.tag))
                gameObject.tag = pc.gameObject.tag;

            // Me aseguro de que el collider esté apagado así la veterinaria puede atravesar a sus propios gatitos aliados
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private void Update()
    {
        if (_isDead) return;
        if (_player == null) FindPlayer();

        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;

        // Busco si hay algún enemigo corrupto vivo cerca
        _currentTarget = FindClosestEnemy();

        if (_currentTarget != null)
        {
            FightEnemy();
        }
        else if (_player != null)
        {
            FollowPlayer();
        }

        if (_animator != null)
        {
            float speed = (_agent != null && _agent.enabled && _agent.isOnNavMesh && !_agent.isStopped)
                ? _agent.velocity.magnitude
                : 0f;
            _animator.SetFloat(SpeedHash, speed);
        }
        else if (transform.childCount > 0)
        {
            // Si el modelo del gatito no tiene Animator (como Cuti), le doy un saltito suave por código al caminar
            Transform model = transform.GetChild(0);
            bool moving = _agent != null && _agent.enabled && _agent.isOnNavMesh && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.05f;
            float yHop = moving ? Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.08f : 0f;
            model.localPosition = new Vector3(model.localPosition.x, 0.1f + yHop, model.localPosition.z);
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

            // Miro al enemigo y lo ataco cuando se cumple el cooldown
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

        // Cuando no hay pelea, baja un cambio y me sigue relajado manteniendo distancia
        _agent.speed = _followSpeed;

        float dist = Vector3.Distance(transform.position, _player.position);

        // Si yo camino hacia el gatito y le quedo muy encima (ej. pasando una puerta), se corre un pasito atrás para darme paso
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

    // Armo un círculo verde en el piso alrededor del aliado con LineRenderer para distinguirlo al toque
    private void CreateGreenRing()
    {
        GameObject ringObj = new GameObject("AroAliado");
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = _ringWidth;
        lr.endWidth = _ringWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Uso el shader Sprites/Default que viene siempre en Unity y toma el color directo del LineRenderer
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null)
            lr.material = new Material(sh);

        lr.startColor = _allyRingColor;
        lr.endColor = _allyRingColor;

        int segments = 24;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * _ringRadius, 0f, Mathf.Sin(angle) * _ringRadius));
        }
    }

    private Enemy FindClosestEnemy()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy closest = null;
        float minDist = _enemyDetectRange;

        foreach (Enemy e in enemies)
        {
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

    protected override void Die()
    {
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = true;
        gameObject.SetActive(false);
    }
}

