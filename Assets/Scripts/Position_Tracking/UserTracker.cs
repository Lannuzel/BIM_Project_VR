using UnityEngine;

public class UserTracker : MonoBehaviour
{
    public string userName; // Nom ou ID de l'utilisateur

    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }

    public string GetUserName()
    {
        return userName;
    }
}
