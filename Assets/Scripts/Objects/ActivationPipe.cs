using UnityEngine;

public class ActivationPipe : MonoBehaviour
{
    [SerializeField] private GameObject VFX;
    [SerializeField] private GameObject collisionObject;
    private bool isActive = false;


    public void Toggle()
    {
        if (!isActive)
        {
            VFX.SetActive(true);
            collisionObject.SetActive(true);
            isActive = true;
        }
        else
        {
            VFX.SetActive(false);
            collisionObject.SetActive(false);
            isActive = false;
        }
    }
}
