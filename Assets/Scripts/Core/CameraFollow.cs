/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/30/2026, 10:17:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/26/2026, 3:55:00 PM
 * @Description: Cámara suave que sigue al jugador con zona muerta. La traje del proyecto anterior.
 *   CAMBIOS: dejé el offset en vista top view de 45° (0, 14, -14), achiqué la zona muerta
 *   y cambié los comentarios porque ya no hay barco, es el veterinario.
 * @Copyright: Copyright (©)}) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform _target;

    [Header("Posición")]
    [Tooltip("Altura y distancia de la cámara respecto al jugador (14 y -14 dan 45°)")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 14f, -14f); // CAMBIO: antes era (0, 22, -15) para el barco
    [Tooltip("Qué tan suave sigue la cámara cuando el jugador sale de la zona muerta")]
    [SerializeField] private float _smoothSpeed = 3f;

    [Header("Zona Muerta")]
    [Tooltip("El jugador se puede alejar esta distancia del centro sin que la cámara se mueva")]
    [SerializeField] private float _deadZoneRadius = 1.5f; // CAMBIO: antes era 4, acá las salas son más chicas

    private Vector3 _currentTarget; // el punto que sigue la cámara (no al jugador directo)

    private void Start()
    {
        if (_target != null)
            _currentTarget = _target.position;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        // Solo muevo el punto que sigo si el jugador se salió de la zona muerta
        float dist = Vector3.Distance(
            new Vector3(_target.position.x, 0, _target.position.z),
            new Vector3(_currentTarget.x, 0, _currentTarget.z)
        );

        if (dist > _deadZoneRadius)
        {
            // Lo empujo lo justo para que el jugador quede en el borde de la zona muerta
            Vector3 dir = (_target.position - _currentTarget).normalized;
            _currentTarget = _target.position - dir * _deadZoneRadius;
        }

        // Muevo la cámara suave hacia donde tiene que estar
        Vector3 desiredPos = new Vector3(_currentTarget.x, _target.position.y, _currentTarget.z) + _offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, _smoothSpeed * Time.deltaTime);

        // Siempre mira al jugador
        transform.LookAt(_target);
    }
}
