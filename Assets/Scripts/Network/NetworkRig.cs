using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkRig : NetworkBehaviour
{
    public bool IsLocalNetworkRig => Object.HasStateAuthority && !isSimulated;

    [Header("RigComponents")]
    [SerializeField]
    private NetworkTransform playerTransform;

    [SerializeField]
    private NetworkTransform headTransform;

    [SerializeField]
    private NetworkTransform leftHandTransform;

    [SerializeField]
    private NetworkTransform rightHandTransform;

    HardwareRig hardwareRig;
    private bool isSimulated;

    public override void Spawned()
    {
        base.Spawned();

        // F18 : un joueur simulé n'est pas lié au casque (il reste où il a été posé).
        var pc = GetComponent<PlayerController>();
        isSimulated = pc != null && pc.IsSimulated;

        if (IsLocalNetworkRig)
        {
            hardwareRig = FindObjectOfType<HardwareRig>();
            if (hardwareRig == null)
                Debug.LogError("Missing HardwareRig in the scene");
        }
        // else it means that this is a client
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (isSimulated) return;

        if (GetInput<RigState>(out var input))
        {
            playerTransform.transform.SetPositionAndRotation(input.PlayerPosition, input.PlayerRotation);

            headTransform.transform.SetPositionAndRotation(input.HeadsetPosition, input.HeadsetRotation);

            leftHandTransform.transform.SetPositionAndRotation(input.LeftHandPosition, input.LeftHandRotation);

            rightHandTransform.transform.SetPositionAndRotation(input.RightHandPosition, input.RightHandRotation);
        }
    }

    public override void Render()
    {
        base.Render();
        if (IsLocalNetworkRig && hardwareRig != null)
        {
            playerTransform.transform.SetPositionAndRotation(hardwareRig.playerTransform.position, hardwareRig.playerTransform.rotation);

            headTransform.transform.SetPositionAndRotation(hardwareRig.headTransform.position, hardwareRig.headTransform.rotation);

            leftHandTransform.transform.SetPositionAndRotation(hardwareRig.leftHandTransform.position, hardwareRig.leftHandTransform.rotation);

            rightHandTransform.transform.SetPositionAndRotation(hardwareRig.rightHandTransform.position, hardwareRig.rightHandTransform.rotation);
        }
    }
}
