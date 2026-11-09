namespace Framework.Script.Model
{
    /// <summary>
    /// 所有业务模型（MVC 中的 M 层）的基类。
    ///
    /// 约定：
    /// - 一个业务系统至少有一个主模型（如 RoleSystem），也可以按子域拆成多个模型；
    /// - 模型由 ModelManager 统一 new / 注册 / 启动 / 销毁，业务代码不要自己 new；
    /// - 模型负责：校验规则、读写本系统 Data、广播本系统 Event；
    /// - 模型不负责：界面展示、文件 IO、PlayerPrefs、场景对象。
    ///
    /// 生命周期：
    ///   OnInit  —— 注册到 ModelManager 时执行一次（此时本系统 Data 已经可以取到）
    ///   OnStart —— 全部模型注册完成后执行一次（适合在这里读本系统存档）
    ///   OnDestroy —— ModelManager 销毁时执行一次
    /// </summary>
    public abstract class ModelBase
    {
        private bool m_IsInited;
        private bool m_IsStarted;

        /// <summary>
        /// 是否已经执行过 OnInit
        /// </summary>
        public bool IsInited
        {
            get { return m_IsInited; }
        }

        /// <summary>
        /// 是否已经执行过 OnStart
        /// </summary>
        public bool IsStarted
        {
            get { return m_IsStarted; }
        }

        /// <summary>
        /// 模型名（默认用类型名，便于日志和调试）
        /// </summary>
        public string GetName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// 由 ModelManager 调用，业务侧不要直接调
        /// </summary>
        internal void Init()
        {
            if (m_IsInited)
            {
                return;
            }

            m_IsInited = true;
            OnInit();
        }

        /// <summary>
        /// 由 ModelManager 调用，业务侧不要直接调
        /// </summary>
        internal void Start()
        {
            if (!m_IsInited)
            {
                Init();
            }
            if (m_IsStarted)
            {
                return;
            }

            m_IsStarted = true;
            OnStart();
        }

        /// <summary>
        /// 由 ModelManager 调用，业务侧不要直接调
        /// </summary>
        internal void Destroy()
        {
            if (!m_IsInited)
            {
                return;
            }

            OnDestroy();
            m_IsInited = false;
            m_IsStarted = false;
        }

        #region 生命周期（子类按需重写）

        /// <summary>
        /// 注册到 ModelManager 时执行一次。此时本系统的 Data 已经注册完成，可以取用。
        /// </summary>
        protected internal virtual void OnInit()
        {
        }

        /// <summary>
        /// 全部模型注册完成后执行一次。读存档、初始化依赖其它模型的逻辑放在这里。
        /// </summary>
        protected internal virtual void OnStart()
        {
        }

        /// <summary>
        /// ModelManager 销毁时执行一次，用于退订事件、释放非托管资源。
        /// </summary>
        protected internal virtual void OnDestroy()
        {
        }

        #endregion
    }
}
