using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnAtTheRightPlace : MonoBehaviour
{
    [SerializeField]
    Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = startPosition;
	}
}
