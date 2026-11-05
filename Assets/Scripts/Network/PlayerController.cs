using Fusion;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }

    /// <summary>F18 : avatar créé par l'expérimentateur pour simuler un joueur (pas de logique de session).</summary>
    [Networked] public NetworkBool IsSimulated { get; set; }

    [SerializeField] private TMP_Text nameLabel_recto;
    [SerializeField] private TMP_Text nameLabel_verso;

    private string lastLabel;

    private string Label => IsSimulated ? $"{PlayerName} (simulé)" : PlayerName;

    public override void Spawned()
    {
        // Met à jour le label lorsque le joueur est spawné
        RefreshLabel();
    }

    public override void FixedUpdateNetwork()
    {
        RefreshLabel();
    }

    public override void Render()
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        string label = Label ?? string.Empty;
        if (label == lastLabel) return;
        lastLabel = label;
        if (nameLabel_recto != null) nameLabel_recto.text = label;
        if (nameLabel_verso != null) nameLabel_verso.text = label;
    }
}
