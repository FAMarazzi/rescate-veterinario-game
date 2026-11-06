/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/5/2026, 11:39:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/6/2026, 4:43:48 PM
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

    [Header("Feedback de Daño y Corrupción")]
    [SerializeField] private Color _corruptTint = new Color(0.65f, 0.25f, 0.9f); // tinte violeta mientras está corrupto
    [SerializeField] private Color _damageFlashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.15f;

    [Header("Al Curarse (Vida = 0)")]
    [Tooltip("Si está tildado, al curarse se vuelve aliado y pelea conmigo; si no, queda dormido/manso en el lugar")]
    [SerializeField] private bool _joinAsAllyWhenCured = false;

    protected State _currentState = State.Idle;
    protected NavMeshAgent _agent;
    protected Rigidbody _rb;
    protected Animator _animator;
    protected Transform _target;      // el jugador (o aliado) al que persigue
    protected Entity _targetEntity;
    protected float _attackTimer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Coroutine _flashCoroutine;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            _animator.applyRootMotion = false;

        // Congelo la rotación y la posición Y para que no se caiga ni me levante cuando nos chocamos
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rb.linearDamping = 6f; // que el empujón del melee frene rápido y no patine infinito

        // Estiro la cápsula en Y para que los dardos (que salen a la altura del rifle) no les pasen por arriba a los animalitos petisos
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null && cap.height < 1.5f)
        {
            cap.height = 1.6f;
            cap.center = new Vector3(cap.center.x, 0.8f, cap.center.z);
        }

        CacheRenderers();
        SetRenderersColor(_corruptTint); // arranca teñido de violeta por estar corrupto
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
            UpdateAnimator();
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

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;
        float speed = (_agent != null && _agent.enabled && _agent.isOnNavMesh && !_agent.isStopped)
            ? _agent.velocity.magnitude
            : 0f;
        _animator.SetFloat(SpeedHash, speed);
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
            // Mantengo la velocidad física en cero mientras se mueve con NavMesh para que no empuje al jugador con fuerza infinita
            if (_rb != null && !_rb.isKinematic)
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, 10f * Time.deltaTime);
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

        // Al llegar a 0 de vida ya fue tranquilizado/curado: le devuelvo su color original de animal sano
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        RestoreRenderersColor();

        if (_joinAsAllyWhenCured)
        {
            // Se convierte en aliado y empieza a seguirme y pelear conmigo
            Ally ally = gameObject.GetComponent<Ally>();
            if (ally == null) ally = gameObject.AddComponent<Ally>();
            this.enabled = false;
        }
        else
        {
            // Queda dormido / tranquilo en el piso sin molestar ni bloquear el paso
            if (_animator != null) _animator.enabled = false;
            if (_agent != null) _agent.enabled = false;
            if (_rb != null) _rb.isKinematic = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Lo acuesto de costado para que se note visualmente que quedó dormido
            transform.rotation = transform.rotation * Quaternion.Euler(0f, 0f, 80f);
            StartCoroutine(SleepZzzRoutine());
            this.enabled = false;
        }
    }

    // Genera letras "Z" flotantes arriba del animalito dormido y un leve movimiento de respiración
    private IEnumerator SleepZzzRoutine()
    {
        Transform model = transform.childCount > 0 ? transform.GetChild(0) : null;
        Vector3 baseScale = model != null ? model.localScale : Vector3.one;
        float timer = 0f;

        while (this != null && gameObject.activeInHierarchy)
        {
            // Respiración suave del cuerpo mientras duerme
            if (model != null)
            {
                float breath = 1f + Mathf.Sin(Time.time * 2.5f) * 0.035f;
                model.localScale = new Vector3(baseScale.x * breath, baseScale.y, baseScale.z * breath);
            }

            timer += Time.deltaTime;
            if (timer >= 0.9f)
            {
                timer = 0f;
                StartCoroutine(SpawnSingleZRoutine());
            }

            yield return null;
        }
    }

    private IEnumerator SpawnSingleZRoutine()
    {
        GameObject zObj = new GameObject("Zzz");
        Vector3 startPos = transform.position + new Vector3(Random.Range(-0.15f, 0.15f), 0.5f, 0f);
        zObj.transform.position = startPos;

        TextMesh tm = zObj.AddComponent<TextMesh>();
        tm.text = "Z";
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        Color baseColor = new Color(0.65f, 0.9f, 1f, 1f); // celeste suave de sueño
        tm.color = baseColor;

        Camera cam = Camera.main;
        float duration = 1.6f;
        float t = 0f;

        while (t < duration && zObj != null)
        {
            t += Time.deltaTime;
            float p = t / duration;

            // Sube flotando y haciendo un pequeño zigzag como en los dibujos animados
            float sway = Mathf.Sin(p * Mathf.PI * 2.5f) * 0.18f;
            zObj.transform.position = startPos + new Vector3(sway, p * 1.1f, 0f);

            // Crece un poquito y se va desvaneciendo al final
            float scale = Mathf.Lerp(0.6f, 1.25f, p);
            zObj.transform.localScale = Vector3.one * scale;
            tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - (p * p));

            // Siempre mira hacia la cámara (billboard)
            if (cam != null)
                zObj.transform.rotation = cam.transform.rotation;

            yield return null;
        }

        if (zObj != null)
            Destroy(zObj);
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
        if (!_isDead)
            SetRenderersColor(_corruptTint);
        else
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
