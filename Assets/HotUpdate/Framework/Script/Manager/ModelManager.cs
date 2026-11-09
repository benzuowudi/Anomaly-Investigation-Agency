using System;
using System.Collections.Generic;
using Framework.Script.Base;
using Framework.Script.Model;
using GameMain.Script.Battle;
using GameMain.Script.Explore;
using GameMain.Script.Role;
using GameMain.Script.Skill;

namespace Framework.Script.Manager
{
    /// <summary>
    /// 业务模型管理器：统一注册、启动、获取、销毁所有 M 层模型（ModelBase 子类）。
    ///
    /// 使用方式：
    ///   1. 新系统在 LoadModels() 里补一行 Register&lt;XxxSystem&gt;()；
    ///   2. 业务代码通过 Game.Model.Get&lt;XxxSystem&gt;() 取模型，不要自己 new；
    ///   3. 模型依赖的 Data 由 DataManager 先注册好（Game.StartGame 的预热顺序：Data → Model）。
    ///
    /// 生命周期：
    ///   注册 → OnInit；全部注册完成 → OnStart（模型在这里读本系统存档）；
    ///   管理器销毁 → 逆序 OnDestroy。
    /// </summary>
    public class ModelManager : Singleton<ModelManager>
    {
        /// <summary>类型 → 模型实例</summary>
        private readonly Dictionary<Type, ModelBase> m_ModelDic = new Dictionary<Type, ModelBase>();
        /// <summary>按注册顺序保存，用于启动/销毁/调试遍历</summary>
        private readonly List<ModelBase> m_ModelList = new List<ModelBase>();
        private bool m_Started;

        protected override void OnInit()
        {
            LoadModels();
        }

        /// <summary>
        /// 业务模型注册表：顺序就是 OnInit / OnStart 的执行顺序。
        /// 新系统接入只改这里。
        /// </summary>
        private void LoadModels()
        {
            Register<RoleSystem>();
            Register<BattleSystem>();
            Register<ExploreSystem>();
            // 技能系统无依赖，但 BattleSystem.Tick 会调它，必须注册（否则 TickSkills 直接跳过）
            Register<SkillSystem>();

            // 待迁移的旧系统（迁移时改成 Register<XxxSystem>()）：
            // Register<WarehouseSystem>();
            // Register<TalentAcquisitionSystem>();

            StartAll();
        }

        /// <summary>
        /// 注册一个模型。重复注册返回已有实例。
        /// </summary>
        public T Register<T>() where T : ModelBase, new()
        {
            ModelBase kExist;
            if (m_ModelDic.TryGetValue(typeof(T), out kExist))
            {
                return (T)kExist;
            }

            T kModel = new T();
            m_ModelDic.Add(typeof(T), kModel);
            m_ModelList.Add(kModel);

            kModel.Init();
            if (m_Started)
            {
                kModel.Start();
            }

            Game.Log.LogInfo("[模型] 注册：" + kModel.GetName());
            return kModel;
        }

        /// <summary>
        /// 启动全部已注册模型（只执行一次）。由 LoadModels 在注册完成后调用。
        /// </summary>
        public void StartAll()
        {
            if (m_Started)
            {
                return;
            }

            m_Started = true;
            for (int i = 0; i < m_ModelList.Count; i++)
            {
                m_ModelList[i].Start();
            }
            Game.Log.LogInfo("[模型] 启动完成，共 " + m_ModelList.Count + " 个");
        }

        /// <summary>
        /// 取模型。未注册时打错误日志并返回 null（属于接线错误，不要当成正常分支）。
        /// </summary>
        public T Get<T>() where T : ModelBase
        {
            ModelBase kModel;
            if (!m_ModelDic.TryGetValue(typeof(T), out kModel))
            {
                Game.Log.LogError("[模型] 未注册：" + typeof(T).Name + "，请先在 ModelManager.LoadModels() 中 Register");
                return null;
            }
            return (T)kModel;
        }

        /// <summary>
        /// 是否已注册指定模型
        /// </summary>
        public bool Has<T>() where T : ModelBase
        {
            return m_ModelDic.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 全部已注册模型（按注册顺序，调试/遍历用）
        /// </summary>
        public IReadOnlyList<ModelBase> GetAll()
        {
            return m_ModelList;
        }

        /// <summary>
        /// 销毁全部模型（退出游戏 / 重载存档时用），按注册顺序逆序执行。
        /// </summary>
        public void DestroyAll()
        {
            for (int i = m_ModelList.Count - 1; i >= 0; i--)
            {
                m_ModelList[i].Destroy();
            }

            m_ModelDic.Clear();
            m_ModelList.Clear();
            m_Started = false;
        }
    }
}
