/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/21/2026, 1:06:19 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/6/2026, 2:03:50 PM
 * @Description: El veterinario jugable. Lo traje del PlayerController del proyecto anterior.
 *   CAMBIOS: saqué todo lo del barco (olas, bamboleo, cañones). Ahora uso el Input System nuevo
 *   (WASD y mouse), miro hacia el mouse, click izquierdo es melee con empuje y click derecho
 *   tira un dardo del pool. Mantuve la inercia con _drag y el respawn, que ahora espera un delay.
 * @Copyright: Copyright (©)}) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Hereda de Entity (vida, daño, muerte) y le sumo movimiento, melee y dardos
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : Entity
{
    [Header("Movimiento")]
    [SerializeField] private float _speed = 7f;
    [SerializeField] private float _drag = 8f; // qué tan rápido frena la inercia cuando suelto las teclas
    [SerializeField] private float _turnSpeed = 15f; // qué tan rápido gira el cuerpo hacia donde camina

    [Header("Melee (click izquierdo o barra espaciadora)")]
    [SerializeField] private float _meleeDamage = 20f;
    [SerializeField] private float _meleeRange = 2.2f;
    [SerializeField] private float _meleeAngle = 140f;     // qué tan ancho es el golpe, en grados
    [SerializeField] private float _meleePushForce = 8f;   // cuánto empujo al enemigo
    [SerializeField] private float _meleeCooldown = 0.5f;

    [Header("Dardos (apuntar con click derecho, disparar con click izquierdo)")]
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private float _shootCooldown = 0.45f;
    [SerializeField] private float _aimSpeedMultiplier = 0.72f; // camino más lento mientras apunto
    [SerializeField] private float _aimYawOffset = 0f; // grados que giro el cuerpo al apuntar para que el arma de la animación quede sobre el mouse (probar con valores + y -)
    [SerializeField] private GameObject _weaponIdle;   // arma ubicada para cuando apunta quieta (se oculta sola si no apunto)
    [SerializeField] private GameObject _weaponMoving; // opcional: otra copia del arma ubicada para cuando apunta caminando

    [Header("Animación")]
    [SerializeField] private Animator _animator; // si lo dejo vacío lo busco en el modelo hijo
    [SerializeField] private float _animDamp = 0.1f; // suaviza el cambio entre quieto, caminar y correr

    [Header("Interacción y Botiquines (E para interactuar, Q para curarse)")]
    [SerializeField] private float _interactRange = 2.6f;
    [SerializeField] private int _medkits = 1; // arranco con 1 botiquín de emergencia en la mochila
    [SerializeField] private float _medkitHealAmount = 45f;

    [Header("Vidas y Respawn")]
    [SerializeField] private int _maxLives = 3;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private float _respawnDelay = 2.2f; // tiempo para que se vea completa la animación de caída antes del fundido
    [SerializeField] private float _respawnInvulnerabilityDuration = 2.5f; // segundos de escudo/parpadeo al reaparecer

    public int CurrentLives => _currentLives;
    public int MaxLives => _maxLives;
    public int Medkits => _medkits;

    public event Action<int, int> OnLivesChanged;
    public event Action<int> OnMedkitsChanged;
    public event Action OnOutOfLives; // avisa al GameManager cuando pierdo las 3 vidas

    private int _currentLives;

    private Rigidbody _rb;
    private Camera _cam;
    private Vector3 _currentVelocity; // la velocidad actual del jugador (así hay inercia)
    private float _meleeTimer;
    private float _shootTimer;
    private float _invulnerableTimer;
    private bool _controlEnabled = true;
    private bool _isAiming; // true mientras mantengo el click derecho
    private Vector3 _aimPoint; // el punto del piso donde está el mouse, el dardo va hacia ahí
    private float _groundY; // me guardo la altura inicial del piso para que al respawnear no quede flotando
    private Vector3 _initialModelLocalPos;
    private CanvasGroup _respawnFadeGroup;
    private UnityEngine.UI.Text _respawnFadeText;
    private static readonly RaycastHit[] _interactRayHits = new RaycastHit[12];

    // Hasheo los nombres de los parámetros del Animator (es más rápido que usar strings)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MeleeHash = Animator.StringToHash("Melee");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int AimingHash = Animator.StringToHash("Aiming");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    protected override void Awake()
    {
        base.Awake();
        _currentLives = _maxLives;
        _groundY = transform.position.y;
        _rb = GetComponent<Rigidbody>();
        // Congelo rotación y también la altura (Y) para que al chocar contra cápsulas de enemigos no salga volando por el aire
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Material sin fricción para patinar/deslizar suave contra paredes y marcos de puertas sin trabarse
        PhysicsMaterial frictionless = new PhysicsMaterial("PlayerFrictionless")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        foreach (Collider col in GetComponents<Collider>())
        {
            if (!col.isTrigger)
                col.material = frictionless;
        }
        _cam = Camera.main;
        if (_shootPoint == null) _shootPoint = transform; // si no puse punto de disparo, salen desde el centro
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_animator != null) _initialModelLocalPos = _animator.transform.localPosition;

        // Arranco con el arma guardada en el bolsillo
        UpdateWeaponVisibility();
    }

    private void Update()
    {
        if (_invulnerableTimer > 0f)
            _invulnerableTimer -= Time.deltaTime;

        if (!_controlEnabled || GameManager.IsMenuOpen || Time.timeScale <= 0f) return;

        // CAMBIO: el cuerpo gira hacia donde camina, salvo que esté apuntando (click derecho mantenido):
        // ahí mira al mouse todo el tiempo
        _isAiming = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (_isAiming) AimAtMouse(true);
        SetAnimBool(AimingHash, _isAiming);
        UpdateWeaponVisibility();

        HandleAttacks();
        HandleInteraction();
    }

    // Con la tecla E abro jaulas o saco botiquines de los armarios, y con la Q uso un botiquín para curarme
    private void HandleInteraction()
    {
        if (Keyboard.current == null) return;

        // Tecla Q: utilizar un botiquín guardado si me falta vida
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryUseMedkit();
        }

        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _interactRange);
        IInteractable closest = null;
        float minDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                if (interactable is MedicineCabinet cab && !cab.CanInteractWith(transform.position))
                    continue;

                if (IsBlockedByWall(hit.bounds.center))
                    continue;

                float d = Vector3.Distance(transform.position, hit.bounds.center);
                if (d < minDist)
                {
                    minDist = d;
                    closest = interactable;
                }
            }
        }

        if (closest != null)
            closest.Interact(this);
    }

    private bool IsBlockedByWall(Vector3 targetCenter)
    {
        Vector3 start = new Vector3(transform.position.x, 0.85f, transform.position.z);
        Vector3 end = new Vector3(targetCenter.x, 0.85f, targetCenter.z);
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 0.05f) return false;

        int count = Physics.RaycastNonAlloc(start, dir / dist, _interactRayHits, dist);
        for (int i = 0; i < count; i++)
        {
            Collider c = _interactRayHits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            if (c.name.StartsWith("Pared"))
                return true;
        }
        return false;
    }

    public void AddMedkit(int count = 1)
    {
        _medkits += count;
        OnMedkitsChanged?.Invoke(_medkits);
    }

    public bool TryUseMedkit()
    {
        if (_medkits <= 0 || _isDead || _currentHealth >= _maxHealth) return false;

        _medkits--;
        Heal(_medkitHealAmount);
        OnMedkitsChanged?.Invoke(_medkits);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHeal();
        return true;
    }

    // Muestro el arma solo mientras apunto; si puse dos posiciones distintas (quieta vs caminando), alterna sola
    private void UpdateWeaponVisibility()
    {
        bool isMoving = _currentVelocity.sqrMagnitude > 0.05f;

        if (_weaponMoving != null)
        {
            if (_weaponIdle != null)
                _weaponIdle.SetActive(_isAiming && !isMoving);
            _weaponMoving.SetActive(_isAiming && isMoving);
        }
        else if (_weaponIdle != null)
        {
            _weaponIdle.SetActive(_isAiming);
        }
    }

    private void FixedUpdate()
    {
        if (!_controlEnabled)
        {
            // si el jugador está muerto lo freno en seco (menos la Y para que siga cayendo si hace falta)
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            SetAnimFloat(SpeedHash, 0f);
            return;
        }
        HandleMovement();
    }

    // CAMBIO: antes usaba Input.GetAxisRaw (sistema viejo), ahora leo las teclas del Input System nuevo
    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
        }

        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        if (direction.sqrMagnitude > 0.01f)
        {
            float currentSpeed = _isAiming ? _speed * _aimSpeedMultiplier : _speed;
            _currentVelocity = direction * currentSpeed;

            // Giro suave hacia donde me estoy moviendo (si estoy apuntando, el cuerpo mira al mouse y no gira acá)
            if (!_isAiming)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _turnSpeed * Time.fixedDeltaTime);
            }
        }
        else
            // Inercia: si no toco nada voy frenando de a poco
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, _drag * Time.fixedDeltaTime);

        _rb.linearVelocity = new Vector3(_currentVelocity.x, _rb.linearVelocity.y, _currentVelocity.z);

        // Le paso la velocidad al Animator: 0 = quieto, hasta _speed = corriendo
        SetAnimFloat(SpeedHash, _currentVelocity.magnitude);
    }

    // Solo toco el parámetro si existe en el controller, así no me llena la consola de warnings
    private void SetAnimFloat(int hash, float value)
    {
        if (_animator != null && HasParam(hash, AnimatorControllerParameterType.Float))
            _animator.SetFloat(hash, value, _animDamp, Time.deltaTime);
    }

    private void SetAnimBool(int hash, bool value)
    {
        if (_animator != null && HasParam(hash, AnimatorControllerParameterType.Bool))
            _animator.SetBool(hash, value);
    }

    private void SetAnimTrigger(int hash)
    {
        if (_animator != null && HasParam(hash, AnimatorControllerParameterType.Trigger))
            _animator.SetTrigger(hash);
    }

    private bool HasParam(int hash, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in _animator.parameters)
            if (p.nameHash == hash && p.type == type) return true;
        return false;
    }

    // CAMBIO: nuevo. Como es top view, miro hacia donde está el mouse sobre el piso, pero solo cuando ataco
    private void AimAtMouse(bool applyAimOffset = false)
    {
        if (_cam == null || Mouse.current == null) return;

        // Tiro un rayo desde la cámara hasta el mouse a la altura del arma (con cámara a 45°, usar el piso desfasa el tiro al apuntar abajo)
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Transform origin = GetActiveShootPoint();
        Plane aimPlane = new Plane(Vector3.up, origin.position);

        if (aimPlane.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            _aimPoint = point;
            Vector3 look = point - transform.position;
            look.y = 0f; // solo gira de costado, que no se incline
            if (look.sqrMagnitude > 0.01f)
            {
                // CAMBIO: al apuntar sumo _aimYawOffset porque la animación tiene el arma girada respecto del cuerpo
                float yaw = applyAimOffset ? _aimYawOffset : 0f;
                transform.rotation = Quaternion.LookRotation(look) * Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }

    private void HandleAttacks()
    {
        // Bajo los cooldowns en cada frame
        if (_meleeTimer > 0f) _meleeTimer -= Time.deltaTime;
        if (_shootTimer > 0f) _shootTimer -= Time.deltaTime;

        Mouse mouse = Mouse.current;
        Keyboard kb = Keyboard.current;

        // Barra espaciadora: empujón rápido en cualquier momento (incluso mientras apunto) para sacarme gatos de encima
        if (kb != null && kb.spaceKey.wasPressedThisFrame && _meleeTimer <= 0f)
        {
            if (!_isAiming) AimAtMouse();
            MeleeAttack();
            _meleeTimer = _meleeCooldown;
            return;
        }

        if (mouse == null) return;

        // Click izquierdo: si estoy apuntando disparo el dardo, si no pego con el melee
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (_isAiming)
            {
                if (_shootTimer <= 0f)
                {
                    ShootDart();
                    _shootTimer = _shootCooldown;
                }
            }
            else if (_meleeTimer <= 0f)
            {
                AimAtMouse(); // giro hacia el mouse antes de pegar
                MeleeAttack();
                _meleeTimer = _meleeCooldown;
            }
        }
    }

    // CAMBIO: nuevo. Golpe cuerpo a cuerpo, daño y empujo todo lo que esté adelante mío en un cono
    private void MeleeAttack()
    {
        SetAnimTrigger(MeleeHash);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMelee();
        Collider[] hits = Physics.OverlapSphere(transform.position, _meleeRange);

        // Uso la dirección hacia el mouse para que el cono de golpe coincida con donde miro aunque tenga _aimYawOffset
        Vector3 forwardDir = (_aimPoint - transform.position);
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.01f) forwardDir = transform.forward;
        forwardDir.Normalize();

        foreach (Collider hit in hits)
        {
            // No se golpea a sí mismo ni a los de su mismo bando
            if (hit.transform == transform || hit.CompareTag(gameObject.tag)) continue;

            // Solo cuenta si está dentro del ángulo del golpe (adelante mío)
            Vector3 toTarget = hit.transform.position - transform.position;
            toTarget.y = 0f;
            if (Vector3.Angle(forwardDir, toTarget) > _meleeAngle * 0.5f) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(_meleeDamage);

            // Si es un Enemy con NavMeshAgent, uso ApplyKnockback para pausarle el agente y correrlo hacia atrás de verdad
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyKnockback(toTarget.normalized, 2.6f, 0.55f);
            }
            else
            {
                Rigidbody targetRb = hit.attachedRigidbody;
                if (targetRb != null && !targetRb.isKinematic)
                    targetRb.AddForce(toTarget.normalized * _meleePushForce, ForceMode.Impulse);
            }
        }
    }

    // Tiro un dardo sacándolo del pool en vez de hacer Instantiate
    private void ShootDart()
    {
        SetAnimTrigger(ShootHash);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDartShoot();
        if (BulletPool.Instance == null) return;

        Transform origin = GetActiveShootPoint();

        // CAMBIO: la dirección va desde el punto de disparo hasta el mouse (antes usaba transform.forward
        // y, como el arma está corrida del centro, el dardo salía en diagonal)
        Vector3 dir = _aimPoint - origin.position;
        dir.y = 0f; // el dardo viaja plano
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;

        // Si un gato (enemigo o aliado corrompido) ya se me metió recontra encima, hago que el dardo nazca desde el centro así le pega igual a quemarropa
        Vector3 spawnPos = origin.position;
        Collider[] closeHits = Physics.OverlapSphere(transform.position + Vector3.up * 0.8f, 1.35f);
        foreach (Collider c in closeHits)
        {
            Enemy closeEnemy = c.GetComponentInParent<Enemy>();
            Ally closeCorruptAlly = c.GetComponentInParent<Ally>();
            Transform targetTr = null;

            if (closeEnemy != null && !closeEnemy.IsDead)
                targetTr = closeEnemy.transform;
            else if (closeCorruptAlly != null && closeCorruptAlly.IsCorrupted)
                targetTr = closeCorruptAlly.transform;

            if (targetTr != null)
            {
                Vector3 toEnemy = targetTr.position - transform.position;
                toEnemy.y = 0f;
                if (Vector3.Angle(dir, toEnemy) < 55f)
                {
                    dir = toEnemy.normalized;
                    spawnPos = new Vector3(transform.position.x, origin.position.y, transform.position.z);
                    break;
                }
            }
        }

        BulletController bc = BulletPool.Instance.Get(spawnPos, Quaternion.LookRotation(dir));
        bc.Launch(dir, gameObject.tag);
    }

    // Busco el punto de salida en el arma que esté activa en este momento (quieta o caminando)
    private Transform GetActiveShootPoint()
    {
        GameObject activeWeapon = null;
        if (_weaponMoving != null && _weaponMoving.activeSelf)
            activeWeapon = _weaponMoving;
        else if (_weaponIdle != null && _weaponIdle.activeSelf)
            activeWeapon = _weaponIdle;

        if (activeWeapon != null)
        {
            // Si le puse un hijo "ShootPoint" en la punta del cañón a esa arma, uso ese
            Transform tip = activeWeapon.transform.Find("ShootPoint");
            if (tip != null) return tip;
            return activeWeapon.transform;
        }

        return _shootPoint != null ? _shootPoint : transform;
    }

    public override void TakeDamage(float amount)
    {
        // Si recién respawneó, tiene unos segundos de escudo invulnerable para no recibir daño al instante
        if (_invulnerableTimer > 0f) return;
        base.TakeDamage(amount);
    }

    // Piso el Die de Entity: reproduzco la animación de muerte, descuento 1 vida y hago transición de respawn (o derrota)
    protected override void Die()
    {
        _controlEnabled = false;
        _isAiming = false;
        UpdateWeaponVisibility();
        _currentVelocity = Vector3.zero;
        if (_rb != null)
            _rb.linearVelocity = Vector3.zero;

        SetAnimFloat(SpeedHash, 0f);
        SetAnimBool(AimingHash, false);
        SetAnimBool(DeadHash, true);

        // Alejo un poquito a los gatos que estaban pegados para que el cuerpo de la veterinaria no caiga encima de un michi
        Collider[] closeCats = Physics.OverlapSphere(transform.position, 2.1f);
        foreach (Collider col in closeCats)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                Vector3 pushDir = enemy.transform.position - transform.position;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.01f) pushDir = -transform.forward;
                enemy.ApplyKnockback(pushDir.normalized, 1.8f, 1.1f);
            }
        }

        // Bajo suavemente solo el modelo visual (_animator.transform) para que quede bien apoyada en el piso sin tocar su CapsuleCollider
        StartCoroutine(KeepDeadModelOnFloorRoutine());

        _currentLives = Mathf.Max(0, _currentLives - 1);
        OnLivesChanged?.Invoke(_currentLives, _maxLives);

        if (_currentLives > 0)
        {
            StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            StartCoroutine(OutOfLivesAfterDeathAnimRoutine());
        }
    }

    private IEnumerator KeepDeadModelOnFloorRoutine()
    {
        if (_animator == null) yield break;
        SkinnedMeshRenderer[] smrs = _animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (smrs == null || smrs.Length == 0) yield break;

        // Espero un instante mientras empieza la caída y después apoyo el punto más bajo de su malla al ras del piso (y = 0.02)
        yield return new WaitForSeconds(0.25f);
        while (_isDead && _animator != null)
        {
            float minY = float.MaxValue;
            bool found = false;
            for (int i = 0; i < smrs.Length; i++)
            {
                if (smrs[i] != null && smrs[i].enabled)
                {
                    if (smrs[i].bounds.min.y < minY)
                    {
                        minY = smrs[i].bounds.min.y;
                        found = true;
                    }
                }
            }

            if (found)
            {
                float targetFloorY = 0.02f;
                float diff = targetFloorY - minY;
                Vector3 pos = _animator.transform.position;
                pos.y = Mathf.Lerp(pos.y, pos.y + diff, 14f * Time.deltaTime);
                _animator.transform.position = pos;
            }
            yield return null;
        }
    }

    private IEnumerator OutOfLivesAfterDeathAnimRoutine()
    {
        // Dejo que se vea la animación de caída completa antes de mostrar el cartel de derrota
        yield return new WaitForSeconds(Mathf.Max(2.0f, _respawnDelay));
        OnOutOfLives?.Invoke();
    }

    private IEnumerator RespawnAfterDelay()
    {
        // 1) Se ve la animación de la veterinaria cayendo al piso en el lugar
        yield return new WaitForSeconds(Mathf.Max(1.8f, _respawnDelay));

        // 2) Fundido suave a oscuro para que no sea un salto brusco de cámara ni de posición
        EnsureRespawnFadeUI();
        if (_respawnFadeText != null)
            _respawnFadeText.text = $"REINGRESANDO A LA CLÍNICA...\n♥ Vidas restantes: {_currentLives} / {_maxLives}";

        yield return StartCoroutine(AnimateRespawnFade(0f, 0.94f, 0.38f));

        // 3) Mientras la pantalla está oscura, la reposiciono en la entrada, centro la cámara y la pongo de pie
        RespawnAtSpawnPoint();
        yield return new WaitForSeconds(0.18f);

        // 4) Fundido de regreso a la clínica + onda expansiva + parpadeo de invulnerabilidad temporal
        StartCoroutine(PlayRespawnRingAndBlinkRoutine());
        yield return StartCoroutine(AnimateRespawnFade(0.94f, 0f, 0.42f));
    }

    private void RespawnAtSpawnPoint()
    {
        ResetHealth();
        _currentVelocity = Vector3.zero;
        if (_rb != null)
            _rb.linearVelocity = Vector3.zero;

        SetAnimBool(DeadHash, false);
        SetAnimFloat(SpeedHash, 0f);
        if (_animator != null)
        {
            _animator.transform.localPosition = _initialModelLocalPos;
            if (_animator.runtimeAnimatorController != null)
                _animator.Play("Idle", 0, 0f);
        }

        Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        spawnPos.y = _groundY;
        transform.position = spawnPos;

        // Centro la cámara en el punto de spawn durante el fundido para que no pegue un latigazo cruzando el mapa
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
        {
            CameraFollow camFollow = _cam.GetComponent<CameraFollow>();
            if (camFollow != null)
                camFollow.SnapToTarget();
        }

        // Empujo hacia atrás a cualquier gato corrupto que haya quedado cerca del spawn para que no la embosquen al nacer
        Collider[] nearby = Physics.OverlapSphere(spawnPos, 3.8f);
        foreach (Collider col in nearby)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                Vector3 pushDir = enemy.transform.position - spawnPos;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.01f) pushDir = transform.forward;
                enemy.ApplyKnockback(pushDir.normalized, 2.2f, 0.8f);
            }
        }

        _invulnerableTimer = _respawnInvulnerabilityDuration;
        _controlEnabled = true;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHeal();
    }

    private void EnsureRespawnFadeUI()
    {
        if (_respawnFadeGroup != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject overlayObj = new GameObject("RespawnTransitionOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        UnityEngine.UI.Image bg = overlayObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.03f, 0.04f, 0.08f, 1f);
        bg.raycastTarget = false;

        _respawnFadeGroup = overlayObj.AddComponent<CanvasGroup>();
        _respawnFadeGroup.alpha = 0f;
        _respawnFadeGroup.blocksRaycasts = false;
        _respawnFadeGroup.interactable = false;

        GameObject txtObj = new GameObject("RespawnBannerText");
        txtObj.transform.SetParent(overlayObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0.5f, 0.5f);
        txtRt.anchorMax = new Vector2(0.5f, 0.5f);
        txtRt.sizeDelta = new Vector2(700f, 120f);

        _respawnFadeText = txtObj.AddComponent<UnityEngine.UI.Text>();
        _respawnFadeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _respawnFadeText.fontSize = 28;
        _respawnFadeText.fontStyle = FontStyle.Bold;
        _respawnFadeText.alignment = TextAnchor.MiddleCenter;
        _respawnFadeText.color = new Color(0.45f, 1f, 0.88f, 1f);
        _respawnFadeText.raycastTarget = false;
    }

    private IEnumerator AnimateRespawnFade(float from, float to, float duration)
    {
        if (_respawnFadeGroup == null) yield break;
        _respawnFadeGroup.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _respawnFadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _respawnFadeGroup.alpha = to;
        if (to <= 0.001f)
            _respawnFadeGroup.gameObject.SetActive(false);
    }

    // Onda expansiva turquesa en el piso + parpadeo clásico de invulnerabilidad durante _respawnInvulnerabilityDuration
    private IEnumerator PlayRespawnRingAndBlinkRoutine()
    {
        // Aro expansivo en el piso al aparecer
        GameObject ringObj = new GameObject("RespawnPulseRing");
        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 32;
        lr.startWidth = 0.14f;
        lr.endWidth = 0.14f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null) lr.material = new Material(sh);

        Renderer[] playerRenderers = _animator != null
            ? _animator.GetComponentsInChildren<Renderer>(true)
            : GetComponentsInChildren<Renderer>(true);

        float duration = Mathf.Max(0.5f, _respawnInvulnerabilityDuration);
        float elapsed = 0f;
        const float ringDuration = 0.65f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 1) Actualizar onda expansiva durante los primeros 0.65s
            if (ringObj != null)
            {
                if (elapsed <= ringDuration)
                {
                    float t = elapsed / ringDuration;
                    float radius = Mathf.Lerp(0.25f, 2.6f, 1f - Mathf.Pow(1f - t, 3f));
                    Color ringCol = new Color(0.25f, 1f, 0.85f, 1f - t);
                    lr.startColor = ringCol;
                    lr.endColor = ringCol;
                    lr.startWidth = Mathf.Lerp(0.16f, 0.02f, t);
                    lr.endWidth = lr.startWidth;

                    Vector3 center = new Vector3(transform.position.x, transform.position.y + 0.06f, transform.position.z);
                    for (int i = 0; i < 32; i++)
                    {
                        float ang = (i / 32f) * Mathf.PI * 2f;
                        lr.SetPosition(i, center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius));
                    }
                }
                else
                {
                    Destroy(ringObj);
                }
            }

            // 2) Parpadeo clásico de escudo de invulnerabilidad en el modelo de la veterinaria
            bool visiblePhase = Mathf.FloorToInt(elapsed * 11f) % 2 == 0;
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                    playerRenderers[i].enabled = visiblePhase;
            }

            yield return null;
        }

        if (ringObj != null)
            Destroy(ringObj);

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
                playerRenderers[i].enabled = true;
        }
    }
}
