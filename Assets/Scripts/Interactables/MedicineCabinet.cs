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

    private void Awake()
    {
        CreatePromptLabel();
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) _player = pc.transform;
    }

    private void Update()
    {
        if (_medkitsInside <= 0 || _promptObj == null || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool near = dist <= _promptShowDistance;
        if (_promptObj.activeSelf != near)
            _promptObj.SetActive(near);

        if (near && Camera.main != null)
            _promptObj.transform.rotation = Camera.main.transform.rotation;
    }

    public void Interact(PlayerController player)
    {
        if (_medkitsInside <= 0) return;

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

