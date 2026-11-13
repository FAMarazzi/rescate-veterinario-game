/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 11/6/2026, 4:49:30 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 11/11/2026, 5:13:00 PM
 * @Description: Jaula de la veterinaria donde están encerrados los gatitos aliados (como Cuti y Dibu).
 *   Cuando me acerco y toco la E, se abre y el gatito que estaba adentro activa su script Ally
 *   (o se instancia su prefab) para seguirme y ayudarme a pelear contra los gatos corruptos.
 * @Copyright: Copyright (©) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Cage : MonoBehaviour, IInteractable
{
    public static event Action<Cage> OnCageOpened;

    [Header("Gatito Encerrado")]
    [Tooltip("Si ya puse a Dibu o Cuti adentro de la jaula en la escena, lo arrastro acá")]
    [SerializeField] private Ally _cagedAllyInScene;
    [Tooltip("O si prefiero que instancie a Dibu o Cuti de cero al abrirse, pongo su Prefab acá")]
    [SerializeField] private GameObject _allyPrefabToSpawn;
    [Tooltip("Prefab del gatito genérico que sale corriendo a la puerta cuando la jaula no tiene a Dibu ni Cuti")]
    [SerializeField] private GameObject _rescuedKittenPrefab;
    [SerializeField] private Transform _spawnPoint; // punto adelante de la jaula donde sale el gatito
    [Tooltip("Si es mayor a 0, ajusta el tamaño uniforme de Dibu/Cuti al salir de esta jaula (0 = conserva el tamaño que ya tiene en escena/prefab)")]
    [SerializeField] private float _releasedAllyScale = 0f;

    [Header("Feedback Visual")]
    [SerializeField] private string _promptMessage = "[E] Liberar gatito";
    [SerializeField] private float _promptShowDistance = 2.8f;

    public bool IsOpen => _isOpen;

    private bool _isOpen = false;
    private Transform _player;
    private Vector3 _clinicExitPos;
    private GameObject _promptObj;

    private void Awake()
    {
        // Si el gatito ya está puesto adentro de la jaula, le apago el Ally y el NavMeshAgent
        // para que Unity no lo empuje afuera del BoxCollider de la jaula al darle Play
        if (_cagedAllyInScene == null)
            _cagedAllyInScene = GetComponentInChildren<Ally>();

        if (_cagedAllyInScene != null)
        {
            _cagedAllyInScene.enabled = false;
            NavMeshAgent agent = _cagedAllyInScene.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            Collider allyCol = _cagedAllyInScene.GetComponent<Collider>();
            if (allyCol != null) allyCol.enabled = false;
        }

        CreatePromptLabel();
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            _player = pc.transform;
            _clinicExitPos = pc.transform.position; // me guardo la entrada donde arrancó la veterinaria como punto de escape
        }

        // Si en el Inspector no cambié el cartel por defecto y adentro están Dibu o Cuti, pongo su nombre real
        if (_promptMessage == "[E] Liberar gatito")
        {
            string allyName = _cagedAllyInScene != null ? _cagedAllyInScene.name : (_allyPrefabToSpawn != null ? _allyPrefabToSpawn.name : "");
            if (!string.IsNullOrEmpty(allyName))
            {
                _promptMessage = $"[E] Liberar a {allyName}";
                if (_promptObj != null)
                {
                    TextMesh tm = _promptObj.GetComponent<TextMesh>();
                    if (tm != null) tm.text = _promptMessage;
                }
            }
        }
    }

    private void Update()
    {
        if (_isOpen || _promptObj == null || _player == null) return;

        // Muestro el cartelito "[E] Liberar gatito" (o Dibu/Cuti) cuando estoy cerca de la jaula
        float dist = Vector3.Distance(transform.position, _player.position);
        bool near = dist <= _promptShowDistance;
        if (_promptObj.activeSelf != near)
            _promptObj.SetActive(near);

        if (near && Camera.main != null)
            _promptObj.transform.rotation = Camera.main.transform.rotation;
    }

    public void Interact(PlayerController player)
    {
        if (_isOpen) return;
        _isOpen = true;

        if (_promptObj != null)
            Destroy(_promptObj);

        Vector3 exitPos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position + (player.transform.position - transform.position).normalized * 1.1f;

        if (_cagedAllyInScene != null)
        {
            // Lo saco de adentro de la jaula conservando su tamaño real (o usando el override del Inspector) y enderezando la rotación
            _cagedAllyInScene.transform.SetParent(null, true);
            _cagedAllyInScene.ApplyReleaseScale(_releasedAllyScale);

            // Si la jaula estaba rotada (ej: Consultorio2_Jaula a -90°), conservo la orientación derecha que ya tenía el Model hijo
            bool wasTiltedByCage = Vector3.Angle(_cagedAllyInScene.transform.up, Vector3.up) > 15f;
            Transform modelChild = (wasTiltedByCage && _cagedAllyInScene.transform.childCount > 0)
                ? _cagedAllyInScene.transform.GetChild(0)
                : null;
            if (modelChild != null && modelChild.name == "HealthBar_Root")
                modelChild = null;
            Quaternion savedModelWorldRot = modelChild != null ? modelChild.rotation : Quaternion.identity;

            Vector3 lookDir = player.transform.position - transform.position;
            lookDir.y = 0f;
            _cagedAllyInScene.transform.rotation = lookDir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDir.normalized)
                : Quaternion.identity;

            if (modelChild != null)
                modelChild.rotation = savedModelWorldRot;

            exitPos.y = player.transform.position.y;
            if (NavMesh.SamplePosition(exitPos, out NavMeshHit navHit, 3.5f, NavMesh.AllAreas))
                exitPos = navHit.position;

            _cagedAllyInScene.transform.position = exitPos;

            NavMeshAgent agent = _cagedAllyInScene.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(exitPos);
            }

            _cagedAllyInScene.SnapModelToFloor();
            _cagedAllyInScene.enabled = true;
        }
        else if (_allyPrefabToSpawn != null)
        {
            GameObject spawnedObj = Instantiate(_allyPrefabToSpawn, exitPos, Quaternion.identity);
            Ally spawnedAlly = spawnedObj.GetComponent<Ally>();
            if (spawnedAlly != null)
            {
                spawnedAlly.ApplyReleaseScale(_releasedAllyScale);
                spawnedAlly.SnapModelToFloor();
            }
        }
        else
        {
            // Es una jaula común: sale un gatito sano corriendo hacia la puerta de salida de la clínica
            SpawnRunningKitten(exitPos);
        }

        OnCageOpened?.Invoke(this);
        StartCoroutine(OpenFeedbackRoutine());
    }

    private void SpawnRunningKitten(Vector3 spawnPos)
    {
        spawnPos.y = 0.02f;

        // Uso el KittenPool (Object Pooling) para reciclar los gatitos rescatados sin hacer Instantiate/Destroy en tiempo real
        if (KittenPool.Instance != null)
        {
            KittenPool.Instance.Get(spawnPos, _clinicExitPos);
            return;
        }

        GameObject prefab = _rescuedKittenPrefab;
        if (prefab == null)
        {
            Enemy anyCat = FindFirstObjectByType<Enemy>();
            if (anyCat != null) prefab = anyCat.gameObject;
        }

        if (prefab == null) return;

        GameObject kitten = Instantiate(prefab, spawnPos, Quaternion.identity);
        kitten.name = "GatitoRescatado";
        kitten.transform.localScale = Vector3.one * 0.85f;

        foreach (Renderer r in kitten.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            Material mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.color = Color.white;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }

        RescuedKitten runner = kitten.AddComponent<RescuedKitten>();
        runner.RunToExit(_clinicExitPos);
    }

    // Pequeño rebote y achique de la jaula para que se note que ya quedó abierta/vacía
    private IEnumerator OpenFeedbackRoutine()
    {
        StartCoroutine(SpawnRescuedTextRoutine());

        Vector3 startScale = transform.localScale;
        float t = 0f;
        float dur = 0.2f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float bump = 1f + Mathf.Sin((t / dur) * Mathf.PI) * 0.15f;
            transform.localScale = startScale * bump;
            yield return null;
        }

        transform.localScale = startScale;
    }

    private IEnumerator SpawnRescuedTextRoutine()
    {
        GameObject txtObj = new GameObject("TextoRescate");
        Vector3 startPos = transform.position + new Vector3(0f, 1.4f, 0f);
        txtObj.transform.position = startPos;

        TextMesh tm = txtObj.AddComponent<TextMesh>();
        tm.text = "¡Gatito liberado!";
        tm.fontSize = 42;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        Color baseColor = new Color(0.3f, 1f, 0.45f, 1f);
        tm.color = baseColor;

        Camera cam = Camera.main;
        float duration = 1.4f;
        float t = 0f;

        while (t < duration && txtObj != null)
        {
            t += Time.deltaTime;
            float p = t / duration;
            txtObj.transform.position = startPos + new Vector3(0f, p * 0.9f, 0f);
            tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - (p * p));
            if (cam != null)
                txtObj.transform.rotation = cam.transform.rotation;
            yield return null;
        }

        if (txtObj != null) Destroy(txtObj);
    }

    private void CreatePromptLabel()
    {
        _promptObj = new GameObject("PromptE");
        _promptObj.transform.SetParent(transform, false);
        _promptObj.transform.position = transform.position + new Vector3(0f, 1.3f, 0f);

        TextMesh tm = _promptObj.AddComponent<TextMesh>();
        tm.text = _promptMessage;
        tm.fontSize = 38;
        tm.characterSize = 0.055f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.95f, 0.4f, 1f);

        _promptObj.SetActive(false);
    }
}

