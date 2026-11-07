using Framework.Script.Manager;
using Framework.Script.Helper;

public partial class Game
{
    /// <summary>
    /// 全部管理器统一改为懒加载单例（Singleton&lt;T&gt;），实例由基类持有。
    /// 这里只保留静态访问入口，业务代码继续写 Game.Data / Game.UI 即可，
    /// 不需要关心实例是什么时候创建的。
    /// </summary>
    public static DataManager Data
    {
        get { return DataManager.Instance; }
    }
    public static EventManager Event
    {
        get { return EventManager.Instance; }
    }
    public static GameManager Manager
    {
        get { return GameManager.Instance; }
    }
    public static ResManager Res
    {
        get { return ResManager.Instance; }
    }
    public static UIManager UI
    {
        get { return UIManager.Instance; }
    }
    public static WebManager Web
    {
        get { return WebManager.Instance; }
    }
    public static LogicManager Logic
    {
        get { return LogicManager.Instance; }
    }

    public static StateManager State
    {
        get { return StateManager.Instance; }
    }

    public static SceneManager Scene
    {
        get { return SceneManager.Instance; }
    }

    public static OperateManager Operate
    {
        get { return OperateManager.Instance; }
    }

    public static LogManager Log
    {
        get { return LogManager.Instance; }
    }

    public static GameHelper Helper
    {
        get { return GameHelper.Instance; }
    }

    public static LanguageManager Language
    {
        get { return LanguageManager.Instance; }
    }
}
