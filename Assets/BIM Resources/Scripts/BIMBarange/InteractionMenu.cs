using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionMenu : MonoBehaviour
{
    public Transform menuPositionRef;
    // Start is called before the first frame update
    void Start()
    {
       // transform.parent = menuPositionRef;
        this.transform.position = menuPositionRef.position; 
    }

    // Update is called once per frame
    void Update()
    {
       
        this.transform.position = menuPositionRef.position;

    }
}
