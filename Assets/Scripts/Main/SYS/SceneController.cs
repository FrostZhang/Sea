using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController
{
    string lasetScene;
    AsyncOperation loadOperation;
    public SceneController()
    {
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    public void Load(string name)
    {
        SceneManager.LoadScene(name);
    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {
        MVC.OnSceneLoad?.Invoke(-1);
    }

    //public async Task RetrunLastScene()
    //{
    //    if (lasetScene != null)
    //    {
    //        await LoadAsync(lasetScene);
    //    }
    //}

    //public async Task LoadAsync(string name)
    //{
    //    if (null != loadOperation)
    //        return;        
    //    lasetScene = SceneManager.GetActiveScene().name;
    //    loadOperation = SceneManager.LoadSceneAsync(name);
    //    loadOperation.allowSceneActivation = false;
    //    while (!loadOperation.isDone)
    //    {
    //        await new WaitForEndOfFrame();
    //        MVC.OnSceneLoad?.Invoke(loadOperation.progress);
    //        if (loadOperation.progress >= 0.9f)
    //        {
    //            float p = 0.9f;
    //            while (p < 1)
    //            {
    //                p += Time.deltaTime * 0.2f;
    //                MVC.OnSceneLoad?.Invoke(p);
    //                await new WaitForEndOfFrame();
    //            }
    //            loadOperation.allowSceneActivation = true;
    //            break;
    //        }
    //    }
    //    loadOperation = null;
    //}
}