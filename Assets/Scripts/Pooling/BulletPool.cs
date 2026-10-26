/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 10/02/2026, 2:40:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/26/2026, 3:55:00 PM
 * @Description: Pool de objetos para las balas. Así no hago Instantiate/Destroy mientras se juega.
 *   CAMBIOS: en Return() agregué un chequeo para no meter dos veces la misma bala en la cola
 *   (pasaba si chocaba y se le acababa el tiempo en el mismo frame).
 * @Copyright: Copyright (©)}) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private int _poolSize = 30;

    private readonly Queue<BulletController> _available = new Queue<BulletController>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Preinstancio todas las balas y las desactivo para después ir usándolas
        for (int i = 0; i < _poolSize; i++)
        {
            GameObject obj = Instantiate(_bulletPrefab, transform);
            obj.SetActive(false);
            BulletController bc = obj.GetComponent<BulletController>();
            _available.Enqueue(bc);
        }
    }

    // Saco una bala libre de la cola, la pongo donde la necesito y la activo
    public BulletController Get(Vector3 position, Quaternion rotation)
    {
        BulletController bc;

        if (_available.Count > 0)
        {
            bc = _available.Dequeue();
        }
        else
        {
            // Si se acabaron las balas del pool creo una más para que no se rompa nada
            GameObject obj = Instantiate(_bulletPrefab, transform);
            bc = obj.GetComponent<BulletController>();
        }

        bc.transform.SetPositionAndRotation(position, rotation);
        bc.gameObject.SetActive(true);
        return bc;
    }

    // Desactivo la bala y la vuelvo a meter en la cola para reusarla
    public void Return(BulletController bc)
    {
        // CAMBIO: si ya estaba desactivada es porque ya la devolvieron, no la encolo de nuevo
        if (!bc.gameObject.activeSelf) return;

        bc.gameObject.SetActive(false);
        bc.transform.SetParent(transform);
        _available.Enqueue(bc);
    }
}
