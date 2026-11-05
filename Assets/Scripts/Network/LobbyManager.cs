using UnityEngine;
using TMPro;
using System.Linq;


public class LobbyManager : MonoBehaviour
{
    [SerializeField] public TMP_Dropdown dropdown;

    private void Start()
    {
        // F18 : rôle choisi dès le tutoriel (ou lors d'une session précédente) présélectionné dans la liste.
        string saved = PlayerPrefs.GetString("PlayerName", string.Empty);
        if (dropdown == null || string.IsNullOrEmpty(saved)) return;
        int index = dropdown.options.FindIndex(o => o.text == saved);
        if (index >= 0)
        {
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }
    }

    public void SavePlayerName()
    {
        string selectedName = dropdown.options[dropdown.value].text;
        
        if (!string.IsNullOrEmpty(selectedName))
        {
            // Enregistrer le nom pour cet utilisateur (par exemple via PlayerPrefs)
            PlayerPrefs.SetString("PlayerName", selectedName);

            Debug.Log($"Nom enregistré : {selectedName}");
        }
        else
        {
            Debug.LogWarning("Veuillez sélectionner un nom valide !");
        }
    }
    
    public void UpdateDropdownOptions()
    {
        var availableOptions = dropdown.options
            .Where(option => NameManager.IsNameAvailable(option.text))
            .ToList();

        dropdown.ClearOptions();
        dropdown.AddOptions(availableOptions);
    }
    public void RemoveSelectedOption()
    {
        string selectedName = dropdown.options[dropdown.value].text;

        // Retirer l'option sélectionnée
        dropdown.options.RemoveAt(dropdown.value);
        dropdown.RefreshShownValue();

        Debug.Log($"Option retirée : {selectedName}");
    }
}
