using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Étape « tracer une ligne » du tutoriel (F4).
/// Une ligne n'est affichée, puis comptée, que si ses deux extrémités sont sur le bloc cible
/// (<see cref="targetRoot"/>, par défaut l'enfant « Table1 »). Une ligne lâchée en dehors est supprimée.
/// Entrée : gâchette intérieure (grip) de la main droite, via OVRInput uniquement (plus de double déclenchement).
/// </summary>
public class DrawLineTutorial : MonoBehaviour
{
    public Transform cameraRef;

    public Transform controller;
    public Camera mainCamera; // Assign the main camera or the camera used for raycasting
    public float rayLength = 30f; // Length of the raycast
    public Material lineMaterial; // Material for the LineRenderer
    private GameObject lineObj;
    public GameObject linePrefab;

    public Transform runTimeLines;

    // Layer mask for raycast targets (optional)
    public LayerMask raycastLayerMask;

    [Header("Validation (F4)")]
    [Tooltip("Racine du bloc sur lequel les lignes doivent être tracées. Si vide : l'enfant nommé « Table1 », sinon cet objet.")]
    public Transform targetRoot;
    [Tooltip("Nombre de lignes valides demandées par l'étape.")]
    public int requiredLines = 4;
    [Tooltip("Texte optionnel pour afficher l'état (ex. « Lignes valides : 2/4 »).")]
    public TMP_Text statusText;

    /// <summary>Nombre de lignes dont les deux extrémités sont sur le bloc (lignes conservées par MeasurementHandler).</summary>
    public int ValidLineCount => MeasurementHandler.Instance != null ? MeasurementHandler.Instance.LineCount() : 0;

    /// <summary>Vrai quand le nombre de lignes valides demandé est atteint.</summary>
    public bool IsCompleted => ValidLineCount >= requiredLines;

    /// <summary>Déclenché à chaque changement du nombre de lignes valides.</summary>
    public event Action<int> ValidLineCountChanged;

    /// <summary>Dernier message d'aide (ligne hors du bloc, etc.).</summary>
    public string LastMessage { get; private set; } = string.Empty;

    private LineRenderer currentLine;
    private bool isDrawing = false;
    private bool firstPointOnTarget = false;
    private bool secondPointOnTarget = false;

    private Vector3 firstPoint;
    private Vector3 secondPoint;

    private void Awake()
    {
        if (targetRoot == null)
        {
            var t = transform.Find("Table1");
            targetRoot = t != null ? t : transform;
        }
    }

    private void OnEnable()
    {
        UpdateStatus();
    }

    private void OnDisable()
    {
        // Si l'objet est désactivé en plein tracé, on abandonne la ligne en cours.
        if (isDrawing)
            CancelCurrentLine();
    }

    void Update()
    {
        if (controller == null)
            return;

        // Gâchette intérieure (grip) de la main droite
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            BeginLine();
        }
        else if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            EndLine();
        }

        if (isDrawing)
        {
            UpdateLine();
        }
    }

    private bool RaycastTarget(out RaycastHit hit)
    {
        Ray ray = new Ray(controller.position, controller.rotation * Vector3.forward);

        // On ne retient que les impacts sur le bloc cible.
        int mask = raycastLayerMask.value != 0 ? raycastLayerMask.value : Physics.DefaultRaycastLayers;
        var hits = Physics.RaycastAll(ray, rayLength, mask);
        float best = float.MaxValue;
        hit = default;
        bool found = false;
        foreach (var h in hits)
        {
            if (h.distance < best && IsOnTarget(h))
            {
                best = h.distance;
                hit = h;
                found = true;
            }
        }
        return found;
    }

    private bool IsOnTarget(RaycastHit h)
    {
        return targetRoot == null || h.collider.transform == targetRoot || h.collider.transform.IsChildOf(targetRoot);
    }

    private void BeginLine()
    {
        if (isDrawing)
            return;

        isDrawing = true;
        secondPointOnTarget = false;

        lineObj = Instantiate(linePrefab);
        lineObj.transform.parent = runTimeLines;
        currentLine = lineObj.GetComponent<LineRenderer>();
        lineObj.SetActive(false); // visible seulement quand les deux points sont sur le bloc

        firstPointOnTarget = RaycastTarget(out RaycastHit firstHit);
        if (firstPointOnTarget)
        {
            firstPoint = firstHit.point;
        }
        else
        {
            SetMessage("Commencez la ligne sur le tableau.");
        }
    }

    private void UpdateLine()
    {
        if (currentLine == null || lineObj == null)
            return;

        if (!firstPointOnTarget)
        {
            // Le premier point peut encore être pris dès que le pointeur revient sur le bloc.
            if (RaycastTarget(out RaycastHit retryHit))
            {
                firstPoint = retryHit.point;
                firstPointOnTarget = true;
                SetMessage(string.Empty);
            }
            return;
        }

        if (RaycastTarget(out RaycastHit secondHit))
        {
            secondPoint = secondHit.point;
            secondPointOnTarget = true;
            if (!lineObj.activeSelf)
                lineObj.SetActive(true);
            currentLine.SetPosition(0, firstPoint);
            currentLine.SetPosition(1, secondPoint);
        }
        else
        {
            // Pointeur sorti du bloc : la ligne n'est pas affichée tant qu'il n'y revient pas.
            secondPointOnTarget = false;
            if (lineObj.activeSelf)
                lineObj.SetActive(false);
        }
    }

    private void EndLine()
    {
        if (!isDrawing)
            return;

        isDrawing = false;

        bool valid = firstPointOnTarget && secondPointOnTarget && lineObj != null
                     && Vector3.Distance(firstPoint, secondPoint) > 0.01f;

        if (valid)
        {
            MeasurementHandler.Instance.AddLine(lineObj);
            MeasurementHandler.Instance.lineCount++;
            SetMessage(string.Empty);
            ValidLineCountChanged?.Invoke(ValidLineCount);
        }
        else
        {
            if (lineObj != null)
                Destroy(lineObj);
            SetMessage("Ligne ignorée : les deux extrémités doivent être sur le tableau.");
        }

        lineObj = null;
        currentLine = null;
        UpdateStatus();
    }

    private void CancelCurrentLine()
    {
        isDrawing = false;
        if (lineObj != null)
            Destroy(lineObj);
        lineObj = null;
        currentLine = null;
    }

    /// <summary>Supprime la dernière ligne validée (bouton « Supprimer la dernière mesure »).</summary>
    public void DeleteLastLine()
    {
        if (ValidLineCount <= 0)
            return;

        MeasurementHandler.Instance.DeleteLastLine();
        ValidLineCountChanged?.Invoke(ValidLineCount);
        UpdateStatus();
    }

    private void SetMessage(string message)
    {
        LastMessage = message;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (statusText == null)
            return;

        string status = $"Lignes valides : {ValidLineCount}/{requiredLines}";
        statusText.text = string.IsNullOrEmpty(LastMessage) ? status : $"{status}\n{LastMessage}";
    }
}
