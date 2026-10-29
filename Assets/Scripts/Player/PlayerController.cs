/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/21/2026, 1:06:19 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/29/2026, 5:04:04 PM
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

    [Header("Animación")]
    [SerializeField] private Animator _animator; // si lo dejo vacío lo busco en el modelo hijo
    [SerializeField] private float _animDamp = 0.1f; // suaviza el cambio entre quieto, caminar y correr

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

    // Hasheo los nombres de los parámetros del Animator (es más rápido que usar strings)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MeleeHash = Animator.StringToHash("Melee");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int AimingHash = Animator.StringToHash("Aiming");

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation; // que el jugador no se caiga ni se dé vuelta
        _cam = Camera.main;
        if (_shootPoint == null) _shootPoint = transform; // si no puse punto de disparo, salen desde el centro
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!_controlEnabled) return;

        // CAMBIO: el cuerpo gira hacia donde camina, salvo que esté apuntando (click derecho mantenido):
        // ahí mira al mouse todo el tiempo
        _isAiming = Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (_isAiming) AimAtMouse();
        SetAnimBool(AimingHash, _isAiming);

        HandleAttacks();
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
    private void AimAtMouse()
    {
        if (_cam == null || Mouse.current == null) return;

        // Tiro un rayo desde la cámara hasta el mouse y veo dónde toca el plano del piso
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            Vector3 look = point - transform.position;
            look.y = 0f; // solo gira de costado, que no se incline
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(look);
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

        BulletController bc = BulletPool.Instance.Get(_shootPoint.position, _shootPoint.rotation);
        bc.Launch(transform.forward, gameObject.tag);
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

        // Lo mando al punto de spawn (si me olvidé de ponerlo, al origen)
        transform.position = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        _controlEnabled = true;
    }
}
