/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/5/2026, 11:40:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/5/2026, 11:40:00 AM
 * @Description: Perro corrupto (enemigo a distancia). Hereda de Enemy, frena a media distancia
 *   y dispara proyectiles hacia el jugador reutilizando el BulletPool.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;

public class CorruptDog : Enemy
{
    [Header("Perro Corrupto (Ranged)")]
    [SerializeField] private float _moveSpeed = 3.8f;
    [SerializeField] private Transform _shootPoint;

    protected override void Awake()
    {
        // Tiene más vida que el gato pero es más lento y ataca desde lejos
        _maxHealth = 80f;
        _attackDamage = 20f;
        _detectionRange = 14f;
        _attackRange = 8f;
        _attackCooldown = 1.6f;

        base.Awake();

        if (_agent != null)
            _agent.speed = _moveSpeed;

        if (_shootPoint == null)
            _shootPoint = transform;
    }

    protected override void PerformAttack()
    {
        if (_target == null || BulletPool.Instance == null) return;

        Vector3 spawnPos = _shootPoint != transform
            ? _shootPoint.position
            : transform.position + Vector3.up * 0.6f + transform.forward * 0.6f;

        Vector3 dir = (_target.position - spawnPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;

        BulletController bc = BulletPool.Instance.Get(spawnPos, Quaternion.LookRotation(dir));
        bc.Launch(dir, gameObject.tag);
    }
}

