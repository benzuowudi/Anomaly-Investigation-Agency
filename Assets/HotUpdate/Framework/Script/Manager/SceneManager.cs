using Framework.Script.Base;
using cfg.GameType;
using System;

namespace Framework.Script.Manager
{
    public class SceneManager : Singleton<SceneManager>
    {
        public void ChangeScene(SceneType _nextScene, Action _callback)
        {
            // 切场景前收起所有界面（界面已改为 FairyGUI，不再按类型区分）
            Game.UI.CloseAllUI();
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextScene.ToString());
            _callback?.Invoke();
            //while (!asyncOperation.isDone)
            //{
            //    Debug.Log("场景加载进度：" + asyncOperation.progress);
            //    yield return null;
            //}
            //Debug.Log("场景加载完毕");
        }
    }
}