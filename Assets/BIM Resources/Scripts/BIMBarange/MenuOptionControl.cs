using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuOptionControl : MonoBehaviour
{
    public List<Transform> options = new List<Transform>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void DisactivateAllOptions()
    {
        foreach (Transform t in options)
        {
            t.gameObject.SetActive(false);
        }
    }
    public void ActivateOption(Transform option)
    {
        DisactivateAllOptions();
        option.gameObject.SetActive(true);        
    }
}
