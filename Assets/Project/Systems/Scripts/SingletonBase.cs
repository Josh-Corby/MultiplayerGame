using UnityEngine;

namespace Project
{
    /* things that could be a singleton
     * player character
     * managers
     * settings
     * central ui script
     */

    // seal inherited singletons so they can't be inherited
    public class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        public bool dontDestroy;
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                if (dontDestroy) DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogError($"Found dupicate {typeof(T).Name}. Destroying this version.");
                Destroy(gameObject);
            }
        }
    }
}
