/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/5/2026, 11:39:30 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/5/2026, 11:39:30 AM
 * @Description: Gato corrupto (enemigo melee rápido). Hereda de Enemy, persigue al jugador
 *   y cuando lo tiene cerca le pega un zarpazo con una pequeña animación de embestida por código.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections;
using UnityEngine;

public class CorruptCat : Enemy
{
    [Header("Gato Corrupto (Melee)")]
    [SerializeField] private float _moveSpeed = 4.2f;     // velocidad equilibrada para poder pelear adentro de las salas
    [SerializeField] private float _lungeDistance = 0.4f; // pequeño salto hacia adelante al pegar
    [SerializeField] private float _lungeDuration = 0.12f;
    [SerializeField] private Transform _modelTransform;   // hijo visual para animar el zarpazo sin mover el collider

    private Vector3 _modelInitialLocalPos;

    protected override void Awake()
    {
        // Valores por defecto: si en el Inspector no le puse otra vida/daño, arranca con 50 HP (2 dardos)
        if (_maxHealth <= 0f || Mathf.Approximately(_maxHealth, 100f)) _maxHealth = 50f;
        if (_attackDamage <= 0f) _attackDamage = 12f;
        if (_attackRange <= 0f) _attackRange = 1.6f;
        if (_attackCooldown <= 0f) _attackCooldown = 1.0f;
        _baseMoveSpeed = _moveSpeed;

        base.Awake();

        if (_agent != null)
            _agent.speed = _moveSpeed;

        if (_modelTransform == null && transform.childCount > 0)
            _modelTransform = transform.GetChild(0);

        if (_modelTransform != null)
            _modelInitialLocalPos = _modelTransform.localPosition;
    }

    protected override void PerformAttack()
    {
        if (_target == null) return;

        // Si el objetivo sigue a rango de zarpazo, le aplico el daño
        float dist = Vector3.Distance(transform.position, _target.position);
        if (dist <= _attackRange * 1.3f)
        {
            IDamageable damageable = _target.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(_attackDamage);
        }

        // Animación corta por código para que se note el golpe aunque no tenga Animator todavía
        if (_modelTransform != null && gameObject.activeInHierarchy)
            StartCoroutine(LungeRoutine());
    }

    private IEnumerator LungeRoutine()
    {
        Vector3 forwardLocal = Vector3.forward * _lungeDistance;
        float half = _lungeDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            _modelTransform.localPosition = Vector3.Lerp(_modelInitialLocalPos, _modelInitialLocalPos + forwardLocal, t / half);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            _modelTransform.localPosition = Vector3.Lerp(_modelInitialLocalPos + forwardLocal, _modelInitialLocalPos, t / half);
            yield return null;
        }

        _modelTransform.localPosition = _modelInitialLocalPos;
    }
}

