using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rend un canvas créé en code cliquable avec le pointeur laser des scènes (UIHelpers Meta : OVRInputModule + OVRRaycaster),
/// et avec les interacteurs XRI le cas échéant. Les canvas de la scène utilisent OVRRaycaster ; un simple GraphicRaycaster
/// n'est jamais interrogé par OVRInputModule, d'où des boutons muets.
/// </summary>
public static class XRUiCanvas
{
    public static void MakeInteractive(Canvas canvas)
    {
        if (canvas == null) return;
        var go = canvas.gameObject;

        if (canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        // OVRRaycaster dérive de GraphicRaycaster : un GraphicRaycaster simple en double est inutile.
        var plain = go.GetComponent<GraphicRaycaster>();
        if (plain != null && !(plain is OVRRaycaster))
            Object.Destroy(plain);

        if (go.GetComponent<OVRRaycaster>() == null)
            go.AddComponent<OVRRaycaster>();

        if (go.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
            go.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
    }
}
