using UnityEngine;
using Fusion;
using System;

public class InstantiatePrefabFeedbacks : SimulationBehaviour, IPlayerJoined
{
	public GameObject prefab;

	void IPlayerJoined.PlayerJoined(PlayerRef player)
	{
		if (player == Runner.LocalPlayer && Runner.IsSharedModeMasterClient)
		{
			Debug.Log("[Emery] Instantiating prefab for local player");
			Runner.Spawn(prefab, Vector3.one, Quaternion.identity);
		} else
		{
			Debug.Log($"[Emery] Player is not Server.");
		}
	}
}
