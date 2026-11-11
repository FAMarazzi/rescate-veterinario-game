/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 5:14:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 5:14:00 PM
 * @Description: Gatito sano rescatado de una jaula común. Apenas le abrís la jaula con la E,
 *   sale corriendo contento por el NavMesh hacia la salida de la clínica para ponerse a salvo.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;
using UnityEngine.AI;

public class RescuedKitten : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private Vector3 _exitPoint;
    private float _lifeTimer = 14f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public void RunToExit(Vector3 exitPoint)
    {
        _exitPoint = exitPoint;

        // Si reutilizo el prefab del gato, le apago cualquier script de enemigo/aliado y el collider para que no moleste
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) Destroy(enemy);

        Ally ally = GetComponent<Ally>();
        if (ally != null) Destroy(ally);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        _animator = GetComponentInChildren<Animator>();
        if (_animator != null) _animator.applyRootMotion = false;

        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();

        _agent.enabled = true;
        _agent.speed = 6.2f; // sale corriendo rapidito hacia la puerta de salida
        _agent.Warp(transform.position);
        _agent.SetDestination(_exitPoint);
    }

    private void Update()
    {
        _lifeTimer -= Time.deltaTime;

        if (_animator != null && _agent != null && _agent.isOnNavMesh)
            _animator.SetFloat(SpeedHash, _agent.velocity.magnitude);

        // Cuando llega a la puerta de entrada de la clínica (o pasan unos segundos), ya quedó a salvo afuera
        if (_lifeTimer <= 0f || Vector3.Distance(transform.position, _exitPoint) <= 1.8f)
        {
            Destroy(gameObject);
        }
    }
}

