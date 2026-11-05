using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class LineManager : NetworkBehaviour
{
    public NetworkedLine linePrefab; // Assign this in the Inspector
    private NetworkedLine spawnedLine;

    private Vector3 pointA = new Vector3(0, 0, 2.36f); // Example position
    private Vector3 pointB = new Vector3(0, 0, -1.4f); // Example position
    private XRIBIMInputActions playerInputActions;

    private void Awake()
    {
        playerInputActions = MeasurementHandler.Instance.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();
    }
    private void OnEnable()
    {
        playerInputActions = MeasurementHandler.Instance.playerInputActions;
        playerInputActions.XRIRightInteraction.Enable();
    }

    private void Update()
    {
        if(playerInputActions.XRIRightInteraction.Select.ReadValue<float>() > 0.3)
        {
            Debug.LogError("Input ready to  draw a line**********************************");
        }

        if (playerInputActions.XRIRightInteraction.Select.ReadValue<float>() > 0.3)
        {
            Debug.LogError("trying to draw a line***************");
            DrawNetworkedLine();
        }
    }

    private void DrawNetworkedLine()
    {
        if (spawnedLine == null)
        {
            Fusion.NetworkObject spawnNWLine = null;
            NetworkManager.Instance.Runner.Spawn(linePrefab, Vector3.zero, linePrefab.transform.rotation, NetworkManager.Instance.Runner.LocalPlayer, (runner, obj) =>
            {
                spawnNWLine = obj;
                spawnedLine = spawnNWLine.gameObject.GetComponent<NetworkedLine>();
            });
            updateLinePosition(pointA, pointB);


           // spawnedLine = NetworkManager.Instance.Runner.Spawn(linePrefab, Vector3.zero, Quaternion.identity);
        }

       
    }
    private void updateLinePosition(Vector3 pointA, Vector3 pointB)
    {
        spawnedLine.SetLinePositions(pointA, pointB, true);
    }
}
