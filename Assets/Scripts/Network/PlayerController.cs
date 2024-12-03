using Fusion;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }

    [SerializeField] private TMP_Text nameLabel;

    public override void Spawned()
    {
        // Met à jour le label lorsque le joueur est spawné
        if (!string.IsNullOrEmpty(PlayerName))
        {
            nameLabel.text = PlayerName;
        }
    }

    public override void FixedUpdateNetwork()
    {
    if (nameLabel != null && nameLabel.text != PlayerName)
    {
        nameLabel.text = PlayerName;
    }
    }
}
