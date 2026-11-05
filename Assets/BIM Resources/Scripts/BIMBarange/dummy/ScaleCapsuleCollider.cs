using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

[RequireComponent(typeof(CapsuleCollider))]
public class ScaleCapsuleCollider : MonoBehaviour
{
    public float offsetH = 688f;
    public float offsetR = 27.5f;
    private CapsuleCollider capsule;

    void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        AdjustCollider();
    }

    void Update()
    {
        // Keep updating if the object can change scale at runtime
        AdjustCollider();
    }

    void AdjustCollider()
    {
        Vector3 lossy = transform.lossyScale;
        Debug.Log(lossy.ToString());


        if (transform.name.Contains("Hollow cylinder Reservation Sol"))
        {
            capsule.height = Mathf.Abs(lossy.y) / offsetH;
            capsule.radius = Mathf.Abs(lossy.x) / offsetR;
            Debug.Log("x " + lossy.x + "  " + " y " + lossy.y + "   height " + capsule.height + "  " + capsule.radius);


        }
        else if (transform.name.Contains("Hollow cylinder Reservation Mur"))
        {
            capsule.height = Mathf.Abs(lossy.y) / offsetH;
            capsule.radius = Mathf.Abs(lossy.x) / offsetR;
            Debug.Log("x " + lossy.x + "  " + " y " + lossy.z + "   height " + capsule.height + "  " + capsule.radius);

        }
        

    }
}
