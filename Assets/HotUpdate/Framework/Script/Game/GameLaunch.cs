using Framework.Script.Constant;
using Framework.Script.Manager;
using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    public static void Run()
    {
        if (GameConst.m_GameStart)
        {
            return;
        }

        ResManager kResManager = ResManager.Instance;

        GameObject kDontDestroy = kResManager.LoadAsset<GameObject>("DontDestroy");
        if (kDontDestroy != null)
        {
            Instantiate(kDontDestroy);
        }
        else
        {
            Debug.LogError("[GameLaunch] DontDestroy 加载失败，请检查 Addressables 中是否已配置 Address 为 DontDestroy 的资源");
        }
    }
}