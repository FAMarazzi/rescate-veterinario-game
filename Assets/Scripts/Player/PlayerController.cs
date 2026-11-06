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

    [Header("Melee (click izquierdo)")]
    [SerializeField] private float _meleeDamage = 20f;
    [SerializeField] private float _meleeRange = 1.8f;
    [SerializeField] private float _meleeAngle = 100f;     // qué tan ancho es el golpe, en grados
    [SerializeField] private float _meleePushForce = 8f;   // cuánto empujo al enemigo
    [SerializeField] private float _meleeCooldown = 0.5f;

    [Header("Dardos (apuntar con click derecho, disparar con click izquierdo)")]
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private float _shootCooldown = 0.8f;
    [SerializeField] private float _aimSpeedMultiplier = 0.6f; // camino más lento mientras apunto
    [SerializeField] private float _aimYawOffset = 0f; // grados que giro el cuerpo al apuntar para que el arma de la animación quede sobre el mouse (probar con valores + y -)
    [SerializeField] private GameObject _weaponIdle;   // arma ubicada para cuando apunta quieta (se oculta sola si no apunto)
    [SerializeField] private GameObject _weaponMoving; // opcional: otra copia del arma ubicada para cuando apunta caminando

    [Header("Animación")]
    [SerializeField] private Animator _animator; // si lo dejo vacío lo busco en el modelo hijo
    [SerializeField] private float _animDamp = 0.1f; // suaviza el cambio entre quieto, caminar y correr

    [Header("Interacción (tecla E)")]
    [SerializeField] private float _interactRange = 2.6f;

    [Header("Respawn")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private float _respawnDelay = 2f; // CAMBIO: nuevo, antes aparecía la pantalla de derrota directo

    private Rigidbody _rb;
    private Camera _cam;
    private Vector3 _currentVelocity; // la velocidad actual del jugador (así hay inercia)
    private float _meleeTimer;
    private float _shootTimer;
    private bool _controlEnabled = true;
    private bool _isAiming; // true mientras mantengo el click derecho
    private Vector3 _aimPoint; // el punto del piso donde está el mouse, el dardo va hacia ahí
    private float _groundY; // me guardo la altura inicial del piso para que al respawnear no quede flotando

    // Hasheo los nombres de los parámetros del Animator (es más rápido que usar strings)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MeleeHash = Animator.StringToHash("Melee");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int AimingHash = Animator.StringToHash("Aiming");

    protected override void Awake()
    {
        base.Awake();
        _groundY = transform.position.y;
        _rb = GetComponent<Rigidbody>();
        // Congelo rotación y también la altura (Y) para que al chocar contra cápsulas de enemigos no salga volando por el aire
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _cam = Camera.main;
        if (_shootPoint == null) _shootPoint = transform; // si no puse punto de disparo, salen desde el centro
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        // Arranco con el arma guardada en el bolsillo
        UpdateWeaponVisibility();
    }

    private void Update()
    {
        if (!_controlEnabled) return;

        // CAMBIO: el cuerpo gira hacia donde camina, salvo que esté apuntando (click derecho mantenido):
        // ahí mira al mouse todo el tiempo
        _isAiming = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (_isAiming) AimAtMouse(true);
        SetAnimBool(AimingHash, _isAiming);
        UpdateWeaponVisibility();

        HandleAttacks();
        HandleInteraction();
    }

    // Con la tecla E abro jaulas o puertas que tenga cerca
    private void HandleInteraction()
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _interactRange);
        IInteractable closest = null;
        float minDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                float d = Vector3.Distance(transform.position, hit.transform.position);
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
        Collider[] hits = Physics.OverlapSphere(transform.position, _meleeRange);

        foreach (Collider hit in hits)
        {
            // No se golpea a sí mismo ni a los de su mismo bando
            if (hit.transform == transform || hit.CompareTag(gameObject.tag)) continue;

            // Solo cuenta si está dentro del ángulo del golpe (adelante mío)
            Vector3 toTarget = hit.transform.position - transform.position;
            toTarget.y = 0f;
            if (Vector3.Angle(transform.forward, toTarget) > _meleeAngle * 0.5f) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(_meleeDamage);

            // Lo empujo solo si tiene Rigidbody y no es kinemático, si no la fuerza no hace nada
            Rigidbody targetRb = hit.attachedRigidbody;
            if (targetRb != null && !targetRb.isKinematic)
                targetRb.AddForce(toTarget.normalized * _meleePushForce, ForceMode.Impulse);
        }
    }

    // Tiro un dardo sacándolo del pool en vez de hacer Instantiate
    private void ShootDart()
    {
        SetAnimTrigger(ShootHash);
        if (BulletPool.Instance == null) return;

        Transform origin = GetActiveShootPoint();

        // CAMBIO: la dirección va desde el punto de disparo hasta el mouse (antes usaba transform.forward
        // y, como el arma está corrida del centro, el dardo salía en diagonal)
        Vector3 dir = _aimPoint - origin.position;
        dir.y = 0f; // el dardo viaja plano
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;

        BulletController bc = BulletPool.Instance.Get(origin.position, Quaternion.LookRotation(dir));
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

    // Piso el Die de Entity: el jugador no se destruye, espera un poco y hace respawn
    protected override void Die()
    {
        Debug.Log("Jugador muerto");
        _controlEnabled = false;
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        // Le vuelvo a dar toda la vida y lo dejo quieto
        ResetHealth();
        _currentVelocity = Vector3.zero;

        // Lo mando al punto de spawn pero respetando la Y del piso por si el SpawnPoint quedó alto en la escena
        Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        spawnPos.y = _groundY;
        transform.position = spawnPos;
        _controlEnabled = true;
    }
}
