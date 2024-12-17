using Fusion;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }

    [SerializeField] private TMP_Text nameLabel_recto;
    [SerializeField] private TMP_Text nameLabel_verso;

    public override void Spawned()
    {
        // Met à jour le label lorsque le joueur est spawné
        if (!string.IsNullOrEmpty(PlayerName))
        {
            nameLabel_recto.text = PlayerName;
            nameLabel_verso.text = PlayerName;
        }
    }

    public override void FixedUpdateNetwork()
    {
    if (nameLabel_recto != null && nameLabel_recto.text != PlayerName)
    {
        nameLabel_recto.text = PlayerName;
        nameLabel_verso.text = PlayerName;
    }
    }
}
