using Eflatun.SceneReference;
using Unity.Netcode;

namespace Project
{
    public static class Loader
    {
        public static void LoadNetwork(SceneReference scene)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scene.Name, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
