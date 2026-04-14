using UnityEngine;
using TMPro;

public class EnemyDebugUI : MonoBehaviour
{
    [Header("Referencia al Enemigo")]
    public EnemyBehaviourTree enemyBT;

    [Header("Configuración")]
    public float updateInterval = 0.1f; // segundos

    private TextMeshProUGUI _text;
    private float _timer;

    void Start()
    {
        _text = GetComponent<TextMeshProUGUI>();

        if (enemyBT == null)
        {
            // Buscar enemigo por tag (asumimos "Enemy")
            GameObject enemyObj = GameObject.FindGameObjectWithTag("Enemy");
            if (enemyObj != null)
                enemyBT = enemyObj.GetComponent<EnemyBehaviourTree>();
        }

        if (enemyBT == null)
        {
            _text.text = "<color=red>Enemigo no encontrado</color>";
            enabled = false;
        }
        else
        {
            UpdateText();
        }
    }

    void Update()
    {
        if (enemyBT == null) return;

        _timer += Time.deltaTime;
        if (_timer >= updateInterval)
        {
            _timer = 0f;
            UpdateText();
        }
    }

    void UpdateText()
    {
        string state = enemyBT.CurrentStateName;
        float dist = enemyBT.DistanceToPlayer;
        bool seesPlayer = enemyBT.IsSeeingPlayer;
        bool stunned = enemyBT.IsCurrentlyStunned;
        bool knownLocker = enemyBT.HasKnownLockerWithPlayer;
        string targetInfo = enemyBT.CurrentTargetInfo;
        float remaining = enemyBT.AgentRemainingDistance;
        float speed = enemyBT.AgentSpeed;

        // Formato bonito con colores
        string stateColor = GetStateColor(state);
        string seesColor = seesPlayer ? "green" : "red";
        string stunnedText = stunned ? "<color=orange>SÍ</color>" : "<color=white>No</color>";
        string lockerText = knownLocker ? $"<color=cyan>{enemyBT._knownLockerWithPlayer?.name}</color>" : "<color=white>Ninguna</color>";

        _text.text = $"<b> Estado Enemigo</b>\n" +
                     $"Estado: <color={stateColor}>{state}</color>\n" +
                     $"Distancia: {dist:F2} m\n" +
                     $"Ve al jugador: <color={seesColor}>{(seesPlayer ? "Sí" : "No")}</color>\n" +
                     $"Aturdido: {stunnedText}\n" +
                     $"Taquilla conocida: {lockerText}\n" +
                     $"Objetivo: {targetInfo}\n" +
                     $"Velocidad: {speed:F1} | Dist. restante: {(remaining >= 0 ? remaining.ToString("F1") : "-")}";
    }

    string GetStateColor(string state)
    {
        switch (state)
        {
            case "Wander": return "#AAAAAA";
            case "Chase": return "#FFAA00";
            case "Attack": return "#FF0000";
            case "Stunned": return "#FF00FF";
            case "Investigate": return "#00AAFF";
            case "CheckLocker": return "#00FFAA";
            default: return "white";
        }
    }
}