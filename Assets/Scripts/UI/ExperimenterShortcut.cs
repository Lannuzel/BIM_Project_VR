using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Mode expérimentateur caché (recette du 9 novembre 2026). Le rôle « Experimentateur » n'apparaît plus ni dans le
/// tutoriel ni dans la liste des rôles de la scène d'expérience : il s'obtient par la suite A, A, B, B sur la manette
/// droite, en moins de <see cref="windowSeconds"/> secondes.
/// - Scène d'expérience : le rôle local devient Expérimentateur (<see cref="AdminTools.SetLocalRole"/>), l'option est
///   ajoutée et sélectionnée dans la liste des rôles (pour « Rejoindre ») et le panneau de l'expérimentateur est
///   affiché ; une nouvelle suite masque / affiche le panneau. Le panneau permet de revenir à un rôle de participant.
/// - Tutoriel : le rôle est mémorisé (PlayerPrefs) et rétabli dans la liste au chargement de la scène d'expérience.
/// Installé par <see cref="SessionCore"/> dans toutes les scènes ; aucune modification de scène.
/// </summary>
public class ExperimenterShortcut : MonoBehaviour
{
    [Tooltip("Durée (s) dans laquelle les quatre appuis A, A, B, B doivent s'enchaîner.")]
    public float windowSeconds = 3f;

    private static readonly OVRInput.Button[] Sequence =
    {
        OVRInput.Button.One, OVRInput.Button.One, OVRInput.Button.Two, OVRInput.Button.Two,
    };

    private readonly List<(OVRInput.Button button, float time)> presses = new List<(OVRInput.Button, float)>();
    private bool isTutorial;

    private void Start()
    {
        isTutorial = FindObjectOfType<TutorialManager>(true) != null;
        // Rôle retenu depuis le tutoriel (ou une session précédente) : l'option cachée est rétablie et sélectionnée.
        if (!isTutorial && PlayerPrefs.GetString("PlayerName", string.Empty) == AdminTools.Experimentateur)
            EnsureLobbyOption();
    }

    private void Update()
    {
        Track(OVRInput.Button.One);
        Track(OVRInput.Button.Two);
    }

    private void Track(OVRInput.Button button)
    {
        if (!OVRInput.GetDown(button, OVRInput.Controller.RTouch)) return;
        float now = Time.unscaledTime;
        presses.RemoveAll(p => now - p.time > windowSeconds);
        presses.Add((button, now));
        if (presses.Count < Sequence.Length) return;
        for (int i = 0; i < Sequence.Length; i++)
            if (presses[presses.Count - Sequence.Length + i].button != Sequence[i]) return;
        presses.Clear();
        Activate();
    }

    /// <summary>Passe en mode expérimentateur (ou bascule l'affichage du panneau si on y est déjà).</summary>
    public void Activate()
    {
        if (isTutorial)
        {
            PlayerPrefs.SetString("PlayerName", AdminTools.Experimentateur);
            PlayerPrefs.Save();
            NetworkStatusPanel.Show("Mode expérimentateur : rôle retenu pour la scène d'expérience.", 4f);
            Debug.Log("[ExperimenterShortcut] Rôle Expérimentateur mémorisé depuis le tutoriel.");
            return;
        }

        bool roleChanged = false;
        if (AdminTools.LocalRole != AdminTools.Experimentateur)
        {
            // Crée aussi le panneau via ShowPersonalisedMenu.ApplyRole quand le menu personnalisé existe.
            AdminTools.SetLocalRole(AdminTools.Experimentateur);
            EnsureLobbyOption();
            roleChanged = true;
            Debug.Log("[ExperimenterShortcut] Mode expérimentateur activé (A, A, B, B).");
        }

        var panel = FindObjectOfType<ExperimenterPanel>(true);
        if (panel == null) ExperimenterPanel.Create();
        else panel.gameObject.SetActive(roleChanged || !panel.gameObject.activeSelf);
    }

    /// <summary>Ajoute (si besoin) et sélectionne « Experimentateur » dans la liste des rôles du lobby, pour « Rejoindre ».</summary>
    private static void EnsureLobbyOption()
    {
        foreach (var lobby in FindObjectsOfType<LobbyManager>(true))
        {
            var dropdown = lobby.dropdown;
            if (dropdown == null) continue;
            int index = dropdown.options.FindIndex(o => o.text == AdminTools.Experimentateur);
            if (index < 0)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(AdminTools.Experimentateur));
                index = dropdown.options.Count - 1;
            }
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }
    }
}
