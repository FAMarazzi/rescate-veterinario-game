/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/5/2026, 11:39:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/5/2026, 11:39:00 AM
 * @Description: Clase base abstracta para todos los enemigos (gatos, perros, jefe).
 *   Hereda de Entity y maneja la máquina de estados (Idle, Chase, Attack, Dead) con NavMeshAgent.
 *   Cada enemigo hijo define su propio PerformAttack() (polimorfismo: melee o rango).
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Pido NavMeshAgent para moverse por la clínica y Rigidbody para recibir el empuje del melee
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class Enemy : Entity
{
    protected enum State { Idle, Chase, Attack, Dead }

    [Header("Detección y Distancias")]
    [SerializeField] protected float _detectionRange = 12f; // a qué distancia me ve y empieza a correrme
    [SerializeField] protected float _attackRange = 1.8f;   // a qué distancia frena para atacarme
    [SerializeField] protected float _loseSightRange = 18f; // si me alejo más que esto, vuelve a Idle

    [Header("Ataque")]
    [SerializeField] protected float _attackDamage = 15f;
    [SerializeField] protected float _attackCooldown = 1f;

    [Header("Feedback de Daño")]
    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.15f;

    protected State _currentState = State.Idle;
    protected NavMeshAgent _agent;
    protected Rigidbody _rb;
    protected Transform _target;      // el jugador (o aliado) al que persigue
    protected Entity _targetEntity;
    protected float _attackTimer;

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Coroutine _flashCoroutine;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        // Congelo la rotación física para que no se caiga como un bolo cuando lo empujo
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.linearDamping = 6f; // que el empujón del melee frene rápido y no patine infinito

        CacheRenderers();
    }

    protected virtual void Start()
    {
        // Busco al jugador en la escena por su componente PlayerController
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            _target = player.transform;
            _targetEntity = player;
        }
    }

    protected virtual void Update()
    {
        if (_isDead || _currentState == State.Dead) return;

        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;

        // Si no tengo objetivo o el jugador está muerto, me quedo quieto en Idle
        if (_target == null || (_targetEntity != null && _targetEntity.IsDead))
        {
            ChangeState(State.Idle);
            StopAgent();
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, _target.position);

        switch (_currentState)
        {
            case State.Idle:
                UpdateIdle(distToTarget);
                break;
            case State.Chase:
                UpdateChase(distToTarget);
                break;
            case State.Attack:
                UpdateAttack(distToTarget);
                break;
        }
    }

    protected virtual void UpdateIdle(float distToTarget)
    {
        StopAgent();

        // Si el jugador entra en mi rango de visión, paso a perseguirlo
        if (distToTarget <= _detectionRange)
            ChangeState(State.Chase);
    }

    protected virtual void UpdateChase(float distToTarget)
    {
        // Si se me escapó muy lejos, vuelvo a Idle
        if (distToTarget > _loseSightRange)
        {
            ChangeState(State.Idle);
            return;
        }

        // Si ya lo tengo a tiro de ataque, paso a Attack
        if (distToTarget <= _attackRange)
        {
            ChangeState(State.Attack);
            return;
        }

        // Mientras lo corro, le digo al NavMeshAgent a dónde ir
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);
        }
    }

    protected virtual void UpdateAttack(float distToTarget)
    {
        // Si el jugador se aleja un poquito más allá del rango de ataque, vuelvo a correrlo
        if (distToTarget > _attackRange * 1.15f)
        {
            ChangeState(State.Chase);
            return;
        }

        // Freno para atacar y miro hacia el jugador
        StopAgent();
        LookAtTarget();

        if (_attackTimer <= 0f)
        {
            PerformAttack();
            _attackTimer = _attackCooldown;
        }
    }

    // Cada hijo define su ataque (el gato pega zarpazo de cerca, el perro tira proyectil)
    protected abstract void PerformAttack();

    protected void LookAtTarget()
    {
        if (_target == null) return;
        Vector3 dir = _target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
    }

    protected void StopAgent()
    {
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = true;
    }

    protected void ChangeState(State newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
    }

    // Si me pegan un dardo desde lejos mientras estaba en Idle, me despierto y voy a buscar al jugador
    protected override void OnDamaged(float amount)
    {
        if (_currentState == State.Idle && !_isDead)
            ChangeState(State.Chase);

        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        if (gameObject.activeInHierarchy)
            _flashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    protected override void Die()
    {
        _currentState = State.Dead;
        StopAgent();
        if (_agent != null) _agent.enabled = false;

        // Desactivo el enemigo tras morir (o lo destruyo luego de un instante)
        Destroy(gameObject, 0.2f);
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty("_BaseColor"))
                _originalColors[i] = _renderers[i].sharedMaterial.GetColor("_BaseColor");
            else if (_renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty("_Color"))
                _originalColors[i] = _renderers[i].sharedMaterial.color;
            else
                _originalColors[i] = Color.white;
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        SetRenderersColor(_damageFlashColor);
        yield return new WaitForSeconds(_flashDuration);
        RestoreRenderersColor();
    }

    private void SetRenderersColor(Color c)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            Material mat = _renderers[i].material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            else if (mat.HasProperty("_Color")) mat.color = c;
        }
    }

    private void RestoreRenderersColor()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            Material mat = _renderers[i].material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _originalColors[i]);
            else if (mat.HasProperty("_Color")) mat.color = _originalColors[i];
        }
    }

    // Dibujo los rangos en la Scene para poder ajustarlos a ojo
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}

