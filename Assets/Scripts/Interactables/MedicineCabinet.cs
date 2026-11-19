/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/11/2026, 5:05:00 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 5:05:00 PM
 * @Description: Armario médico de la clínica. Además de decorar, permite sacar un botiquín con la tecla E
 *   (cumpliendo con recolectar ítems para después usarlos con la Q).
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System.Collections;
using UnityEngine;

public class MedicineCabinet : MonoBehaviour, IInteractable
{
    [SerializeField] private int _medkitsInside = 1;
    [SerializeField] private string _promptMessage = "[E] Sacar Botiquín";
    [SerializeField] private float _promptShowDistance = 2.6f;

    private Transform _player;
    private GameObject _promptObj;
    private Collider _myCollider;
    private Vector3 _frontDir = Vector3.forward;
    private static readonly RaycastHit[] _rayHits = new RaycastHit[12];

    private void Awake()
    {
        _myCollider = GetComponentInChildren<Collider>();
        CreatePromptLabel();
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) _player = pc.transform;
        DetectFrontDirection();
    }

    private void DetectFrontDirection()
    {
        // En el FBX del armario (Z-up), el frente apunta hacia -transform.up en el plano XZ
        Vector3 candidate = new Vector3(-transform.up.x, 0f, -transform.up.z);
        if (candidate.sqrMagnitude < 0.01f)
            candidate = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (candidate.sqrMagnitude > 0.01f)
            _frontDir = candidate.normalized;

        // Si el armario está apoyado contra una pared, el frente real es siempre el lado opuesto a esa pared
        Vector3 center = GetCabinetCenter();
        Vector3 origin = new Vector3(center.x, 0.8f, center.z);
        Vector3[] dirs =
        {
            -_frontDir,
            _frontDir,
            new Vector3(transform.right.x, 0f, transform.right.z).normalized,
            new Vector3(-transform.right.x, 0f, -transform.right.z).normalized
        };

        float closestWall = 1.35f;
        for (int d = 0; d < dirs.Length; d++)
        {
            if (dirs[d].sqrMagnitude < 0.01f) continue;
            int count = Physics.RaycastNonAlloc(origin, dirs[d], _rayHits, 1.35f);
            for (int i = 0; i < count; i++)
            {
                Collider c = _rayHits[i].collider;
                if (c == null || c.isTrigger || c.transform.IsChildOf(transform)) continue;
                if (c.name.StartsWith("Pared") && _rayHits[i].distance < closestWall)
                {
                    closestWall = _rayHits[i].distance;
                    _frontDir = -dirs[d];
                }
            }
        }
    }

    private Vector3 GetCabinetCenter()
    {
        return _myCollider != null ? _myCollider.bounds.center : transform.position;
    }

    // Chequeo que la veterinaria esté a distancia, adelante del armario y sin ninguna pared en el medio
    public bool CanInteractWith(Vector3 playerPos)
    {
        if (_medkitsInside <= 0) return false;

        Vector3 cabCenter = GetCabinetCenter();
        Vector3 toPlayer = playerPos - cabCenter;
        toPlayer.y = 0f;

        float dist = toPlayer.magnitude;
        if (dist > _promptShowDistance || dist < 0.001f) return false;

        // 1) No permitir agarrar desde atrás ni de costado trasero del armario
        if (Vector3.Dot(toPlayer / dist, _frontDir) < 0.15f)
            return false;

        // 2) No permitir agarrar a través de una pared
        Vector3 rayStart = new Vector3(cabCenter.x, 0.85f, cabCenter.z);
        Vector3 rayEnd = new Vector3(playerPos.x, 0.85f, playerPos.z);
        Vector3 rayDir = rayEnd - rayStart;
        float rayLen = rayDir.magnitude;
        if (rayLen > 0.05f)
        {
            int count = Physics.RaycastNonAlloc(rayStart, rayDir / rayLen, _rayHits, rayLen);
            for (int i = 0; i < count; i++)
            {
                Collider c = _rayHits[i].collider;
                if (c == null || c.isTrigger) continue;
                if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
                if (_player != null && (c.transform == _player || c.transform.IsChildOf(_player))) continue;
                if (c.name.StartsWith("Pared"))
                    return false;
            }
        }

        return true;
    }

    private void Update()
    {
        if (_medkitsInside <= 0 || _promptObj == null || _player == null) return;

        bool canUse = CanInteractWith(_player.position);
        if (_promptObj.activeSelf != canUse)
            _promptObj.SetActive(canUse);

        if (canUse && Camera.main != null)
            _promptObj.transform.rotation = Camera.main.transform.rotation;
    }

    public void Interact(PlayerController player)
    {
        if (_medkitsInside <= 0 || player == null || !CanInteractWith(player.transform.position)) return;

        _medkitsInside--;
        player.AddMedkit(1);

        if (_medkitsInside <= 0 && _promptObj != null)
            Destroy(_promptObj);

        StartCoroutine(FeedbackRoutine());
    }

    private IEnumerator FeedbackRoutine()
    {
        GameObject txtObj = new GameObject("TextoBotiquin");
        Vector3 startPos = transform.position + new Vector3(0f, 1.8f, 0f);
        txtObj.transform.position = startPos;

        TextMesh tm = txtObj.AddComponent<TextMesh>();
        tm.text = "+1 Botiquín (Usar con Q)";
        tm.fontSize = 38;
        tm.characterSize = 0.055f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        Color baseColor = new Color(0.35f, 1f, 0.55f, 1f);
        tm.color = baseColor;

        Camera cam = Camera.main;
        float duration = 1.4f;
        float t = 0f;

        while (t < duration && txtObj != null)
        {
            t += Time.deltaTime;
            float p = t / duration;
            txtObj.transform.position = startPos + new Vector3(0f, p * 0.8f, 0f);
            tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - (p * p));
            if (cam != null)
                txtObj.transform.rotation = cam.transform.rotation;
            yield return null;
        }

        if (txtObj != null) Destroy(txtObj);
    }

    private void CreatePromptLabel()
    {
        _promptObj = new GameObject("PromptArmario");
        _promptObj.transform.SetParent(transform, false);
        _promptObj.transform.position = transform.position + new Vector3(0f, 1.7f, 0f);

        TextMesh tm = _promptObj.AddComponent<TextMesh>();
        tm.text = _promptMessage;
        tm.fontSize = 36;
        tm.characterSize = 0.05f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(0.5f, 1f, 0.7f, 1f);

        _promptObj.SetActive(false);
    }
}

