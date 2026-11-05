using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

/// <summary>
/// Ajustements appliqués en code à chaque clavier XRI (scène ou global) sans modifier le prefab (F14) :
/// - la touche « Clear » devient une touche de fermeture (« Fermer ») ;
/// - le clavier est agrandi et les étiquettes des touches sont plus grandes pour la lisibilité.
/// </summary>
public class XRKeyboardTweaks : MonoBehaviour
{
    [Header("F14 : fermeture et lisibilité")]
    [Tooltip("Remplacer la touche Clear par une touche de fermeture du clavier.")]
    public bool replaceClearByClose = true;
    public string closeLabel = "Fermer";
    [Tooltip("Facteur d'agrandissement du clavier (1 = inchangé).")]
    public float scaleFactor = 1.3f;
    [Tooltip("Taille minimale des étiquettes des touches (0 = inchangé).")]
    public float minKeyFontSize = 18f;

    [Tooltip("Intervalle (s) de recherche de nouveaux claviers (le clavier global est créé à la demande).")]
    public float rescanInterval = 1f;

    private readonly HashSet<XRKeyboard> done = new HashSet<XRKeyboard>();
    private float nextScan;

    private void Start()
    {
        Scan();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + rescanInterval;
        Scan();
    }

    private void Scan()
    {
        foreach (var kb in Resources.FindObjectsOfTypeAll<XRKeyboard>())
        {
            if (kb == null || !kb.gameObject.scene.IsValid() || kb.hideFlags != HideFlags.None || done.Contains(kb))
                continue;
            Apply(kb);
            done.Add(kb);
        }
    }

    private void Apply(XRKeyboard kb)
    {
        var keys = kb.GetComponentsInChildren<XRKeyboardKey>(true);

        if (replaceClearByClose)
        {
            foreach (var key in keys)
            {
                bool isClear = key.character == "\\cl" || key.keyCode == KeyCode.Clear || key.name == "Clear";
                if (!isClear) continue;

                key.keyCode = KeyCode.None;
                key.character = "\\h";
                key.shiftCharacter = "\\h";
                key.displayIcon = null;
                key.displayCharacter = closeLabel;
                key.shiftDisplayCharacter = closeLabel;
                key.name = "Close";
                if (key.textComponent != null)
                {
                    key.textComponent.enabled = true;
                    key.textComponent.text = closeLabel;
                }
            }
        }

        if (minKeyFontSize > 0f)
        {
            foreach (var key in keys)
            {
                var text = key.textComponent != null ? key.textComponent : key.GetComponentInChildren<TMP_Text>(true);
                if (text != null && text.fontSize < minKeyFontSize)
                {
                    text.enableAutoSizing = false;
                    text.fontSize = minKeyFontSize;
                }
            }
        }

        if (!Mathf.Approximately(scaleFactor, 1f) && scaleFactor > 0f)
            kb.transform.localScale *= scaleFactor;

        if (azertyLayout)
            ApplyAzerty(kb, keys);

        Debug.Log($"[XRKeyboardTweaks] Clavier « {kb.name} » ajusté ({keys.Length} touches).");
    }

    [Header("F15 : disposition AZERTY")]
    [Tooltip("Réorganiser les touches alphabétiques en AZERTY (Q<->A, W<->Z, M en fin de 2e rangée).")]
    public bool azertyLayout = true;

    /// <summary>
    /// Transforme la disposition QWERTY du prefab en AZERTY : permutation des caractères
    /// A/Q et Z/W, et déplacement de la touche M à la fin de la rangée du milieu (QSDFGHJKLM).
    /// Les rangées sont des groupes de mise en page : déplacer la touche suffit.
    /// </summary>
    private void ApplyAzerty(XRKeyboard kb, XRKeyboardKey[] keys)
    {
        XRKeyboardKey Find(string letter)
        {
            foreach (var k in keys)
            {
                if (k == null) continue;
                string c = (k.character ?? string.Empty).Trim();
                if (string.Equals(c, letter, System.StringComparison.OrdinalIgnoreCase) && c.Length == 1)
                    return k;
            }
            return null;
        }

        void SetLetter(XRKeyboardKey key, string letter)
        {
            if (key == null) return;
            key.character = letter.ToLowerInvariant();
            key.shiftCharacter = letter.ToUpperInvariant();
            key.displayCharacter = letter.ToLowerInvariant();
            key.shiftDisplayCharacter = letter.ToUpperInvariant();
            key.name = letter.ToUpperInvariant();
        }

        var q = Find("q"); var a = Find("a"); var w = Find("w"); var z = Find("z"); var m = Find("m"); var l = Find("l");
        if (q == null || a == null || w == null || z == null)
        {
            Debug.LogWarning("[XRKeyboardTweaks] Touches QWERTY introuvables : AZERTY non appliqué.");
            return;
        }

        SetLetter(q, "a"); SetLetter(a, "q");
        SetLetter(w, "z"); SetLetter(z, "w");

        // M passe à la fin de la rangée du milieu (après L).
        if (m != null && l != null && l.transform.parent != null)
        {
            m.transform.SetParent(l.transform.parent, false);
            m.transform.SetSiblingIndex(l.transform.GetSiblingIndex() + 1);
        }

        // Renommer les rangées pour la lisibilité dans la hiérarchie.
        foreach (var row in kb.GetComponentsInChildren<Transform>(true))
        {
            if (row.name == "QWERTYUIOP") row.name = "AZERTYUIOP";
            else if (row.name == "ASDFGHJKL") row.name = "QSDFGHJKLM";
            else if (row.name == "ZXCVBNM") row.name = "WXCVBN";
        }
    }
}
