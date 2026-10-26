/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/21/2026, 1:06:19 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/26/2026, 3:55:00 PM
 * @Description: Dardo tranquilizante que sale del pool. Lo traje del BulletController del proyecto anterior.
 *   CAMBIOS: ahora ignoro los triggers (zonas, sensores) para que no desaparezca al cruzarlos,
 *   y busco el IDamageable también en el padre por si el collider es de un hijo.
 * @Copyright: Copyright (©)}) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletController : MonoBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _lifeTime = 5f; // si no choca con nada, la devuelvo al pool después de esto

    private Rigidbody _rb;

    private string _shooterTag;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;                                          // que no se caiga
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // que no atraviese paredes
    }

    // OnEnable se llama cada vez que el pool la activa, no solo la primera vez que se crea
    private void OnEnable()
    {
        _shooterTag = string.Empty;
        _rb.linearVelocity = Vector3.zero;
        StartCoroutine(ReturnAfterLifetime());
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(_lifeTime);
        ReturnToPool();
    }

    // A este método lo llama el que dispara (jugador o enemigo a distancia), pasándole la dirección
    // y el tag de quien disparó para que la bala no le pegue a su propio bando
    public void Launch(Vector3 direction, string shooterTag)
    {
        _shooterTag = shooterTag;
        _rb.linearVelocity = direction.normalized * _speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Hit(collision.gameObject);
    }

    // Por si el prefab del dardo tiene el collider como Trigger, cubro los dos casos
    private void OnTriggerEnter(Collider other)
    {
        // CAMBIO: si es una zona (otro trigger) lo ignoro, así el dardo no desaparece al pasar por ahí
        if (other.isTrigger) return;
        Hit(other.gameObject);
    }

    private void Hit(GameObject target)
    {
        // Si es del mismo bando que el tirador no hace nada y sigue de largo
        if (!string.IsNullOrEmpty(_shooterTag) && target.CompareTag(_shooterTag))
            return;

        // CAMBIO: uso GetComponentInParent por si el collider está en un hijo del modelo
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(_damage);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        StopAllCoroutines();
        if (BulletPool.Instance != null)
            BulletPool.Instance.Return(this);
        else
            Destroy(gameObject); // si me olvidé de poner el pool en la escena, la destruyo y listo
    }
}
