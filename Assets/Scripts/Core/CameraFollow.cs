/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/30/2026, 10:17:00 AM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/13/2026, 9:14:00 AM
 * @Description: Cámara suave que sigue al jugador con zona muerta. La traje del proyecto anterior.
 *   CAMBIOS: dejé el offset en vista top view de 45° (0, 14, -14), achiqué la zona muerta
 *   y cambié los comentarios porque ya no hay barco, es el veterinario.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
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

    [Header("Fondo Exterior Nocturno")]
    [SerializeField] private Color _nightBackgroundColor = new Color(0.035f, 0.027f, 0.065f, 1f);
    [SerializeField] private Color _exteriorGroundTint = new Color(0.08f, 0.065f, 0.12f, 1f);

    private Vector3 _currentTarget; // el punto que sigue la cámara (no al jugador directo)

    private void Start()
    {
        if (_target != null)
            _currentTarget = _target.position;

        SetupExteriorVisuals();
    }

    private void SetupExteriorVisuals()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _nightBackgroundColor;
        }

        GameObject floorRoot = GameObject.Find("Clinica_Piso");
        if (floorRoot == null || GameObject.Find("Suelo_Exterior_Nocturno") != null) return;

        Renderer[] floorRenderers = floorRoot.GetComponentsInChildren<Renderer>();
        if (floorRenderers.Length == 0) return;

        Bounds combined = floorRenderers[0].bounds;
        Material referenceFloorMat = floorRenderers[0].sharedMaterial;
        for (int i = 1; i < floorRenderers.Length; i++)
        {
            combined.Encapsulate(floorRenderers[i].bounds);
        }

        GameObject extGround = new GameObject("Suelo_Exterior_Nocturno");
        extGround.transform.position = new Vector3(combined.center.x, combined.min.y - 0.03f, combined.center.z);
        extGround.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        extGround.transform.localScale = new Vector3(180f, 180f, 1f);

        MeshFilter mf = extGround.AddComponent<MeshFilter>();
        Mesh quadMesh = new Mesh { name = "ExteriorGroundQuad" };
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f)
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(45f, 0f),
            new Vector2(0f, 45f),
            new Vector2(45f, 45f)
        };
        quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        quadMesh.RecalculateNormals();
        mf.sharedMesh = quadMesh;

        MeshRenderer mr = extGround.AddComponent<MeshRenderer>();
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");

        Material extMat = new Material(unlitShader);
        extMat.name = "Mat_SueloExteriorNocturno";
        if (referenceFloorMat != null && referenceFloorMat.mainTexture != null)
            extMat.mainTexture = referenceFloorMat.mainTexture;
        if (extMat.HasProperty("_BaseColor"))
            extMat.SetColor("_BaseColor", _exteriorGroundTint);
        if (extMat.HasProperty("_Color"))
            extMat.color = _exteriorGroundTint;

        mr.sharedMaterial = extMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
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
