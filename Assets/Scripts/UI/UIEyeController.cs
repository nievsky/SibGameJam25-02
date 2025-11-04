using UnityEngine;

public class UIEyeController : MonoBehaviour
{
    [SerializeField] private ManagerLogic manager;
    [SerializeField] private UIPopWindow popWindow;

    private bool lastVisible;
    private bool initialized;

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<ManagerLogic>(true);

        if (popWindow == null)
            popWindow = GetComponent<UIPopWindow>() ?? FindObjectOfType<UIPopWindow>(true);
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    private void Apply(bool force)
    {
        if (manager == null || popWindow == null) return;

        bool visible = manager.IsPlayerVisibleNow;

        if (!force && initialized && visible == lastVisible)
            return;

        lastVisible = visible;
        initialized = true;

        if (visible)
            popWindow.Show();
        else
            popWindow.Hide();
    }
}