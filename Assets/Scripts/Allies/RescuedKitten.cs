/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 5:14:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/12/2026, 8:41:00 AM
 * @Description: Gatito sano rescatado de una jaula común. Apenas le abrís la jaula con la E,
 *   sale del KittenPool (Object Pooling), corre por el NavMesh hacia la salida de la clínica
 *   y al llegar se devuelve al pool sin usar Destroy.
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
        _lifeTimer = 14f;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (_animator == null)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
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

        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
            _agent = gameObject.AddComponent<NavMeshAgent>();

        _agent.enabled = true;
        _agent.speed = 6.2f;
        _agent.Warp(transform.position);
        _agent.SetDestination(_exitPoint);
    }

    private void Update()
    {
        _lifeTimer -= Time.deltaTime;

        float speed = (_agent != null && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;
        if (_animator != null && _animator.runtimeAnimatorController != null)
            _animator.SetFloat(SpeedHash, speed);

        if (transform.childCount > 0)
        {
            Transform model = transform.GetChild(0);
            bool moving = speed > 0.15f;
            float phase = (Time.time + GetInstanceID() * 0.17f) * 16f;
            float yHop = moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.09f : 0f;
            float pitch = moving ? Mathf.Sin(phase) * 8f : 0f;
            model.localPosition = new Vector3(model.localPosition.x, 0.1f + yHop, model.localPosition.z);
            model.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // Cuando llega a la puerta de entrada de la clínica, vuelve al KittenPool en vez de hacer Destroy
        if (_lifeTimer <= 0f || (_exitPoint - transform.position).sqrMagnitude <= 3.24f)
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;

            if (KittenPool.Instance != null)
                KittenPool.Instance.Return(this);
            else
                gameObject.SetActive(false);
        }
    }
}
