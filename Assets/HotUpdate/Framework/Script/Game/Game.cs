using UnityEngine;
using Framework.Script.Constant;
using Framework.Script.UI;

public partial class Game : MonoBehaviour
{

    public Transform m_TLayer;
    public Transform m_MLayer;
    public Transform m_BLayer;
    public Transform m_GridUnits;
    public Transform m_RoleUnits;
    public Transform m_GameCameraTra;
    public Camera m_GameCamera;
    public Camera m_UICamera;
    public Transform m_Pool;

    private void OnEnable()
    {
        StartGame();
    }

    /// <summary>
    /// 程序总入口
    /// </summary>
    private void StartGame()
    {
        WarmUp();

        CreateUI();

        DontDestroyOnLoad(this);

        GameConst.m_GameStart = true;
        Debug.Log("游戏启动成功");
    }

    /// <summary>
    /// 预热：需要在游戏启动时就绪的单例，按依赖顺序手动访问一次即可创建并初始化；
    /// 其余单例（Event / Log / Web / State / Language / Scene / FairyGUI / GameHelper）
    /// 保持懒加载，第一次被业务代码访问时自动创建。
    /// </summary>
    private void WarmUp()
    {
        // GameManager 需要拿到 Game 自身的节点、相机、对象池引用，只能在这里显式绑定
        Manager.Init(this);

        _ = Res;        // Addressables 初始化
        _ = Data;       // 配置表加载 + GameData 注册
        _ = Logic;      // 逻辑注册
        _ = Operate;    // 操作预制体预加载（依赖 Data / Res）
        _ = UI;         // UI 层与 FairyGUI Stage（依赖 Manager）
    }

    /// <summary>
    /// 常驻界面：两个装饰层（背景图 + 网格前景）放在底层，业务界面放在中层，
    /// 这样界面永远画在装饰图之上（窗口首次 ShowWindow 时才加载预制体和 FairyGUI 包）。
    /// </summary>
    private void CreateUI()
    {
        UI.ShowWindow(UIDefine.BackGround, _isHideCurrentUI: false, _layer: Manager.m_BLayer);
        // UI.ShowWindow(UIDefine.ForeGround, _isHideCurrentUI: false, _layer: Manager.m_BLayer);
        // 主菜单要记成"当前界面"，这样点开始游戏 ShowWindow(..., _isHideCurrentUI: true) 才会把它收起来
        UI.ShowWindow(UIDefine.GameStart, _isHideCurrentUI: true);
    }
}
