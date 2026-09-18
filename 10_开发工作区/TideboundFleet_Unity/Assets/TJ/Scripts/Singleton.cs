using UnityEngine;

public class Singleton<Class> : MonoBehaviour where Class : MonoBehaviour
{
    private static Class instance;

    public static Class Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as Class;
        }
    }
}