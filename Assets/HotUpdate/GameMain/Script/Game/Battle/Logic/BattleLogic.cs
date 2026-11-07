using cfg.GameType;
using DG.Tweening;
using Framework.Script.Constant;
using Framework.Script.Logic;
using GameMain.Script.Class;
using GameMain.Script.Data;
using System;
using UnityEngine;

namespace GameMain.Script.Logic
{
    public class BattleLogic : GameLogic
    {
        private float m_StandardLength = 0.0f;//六边形瓦片每一行的间隔
        private MapTileClass m_CurrentTile;
        private GameObject m_CurrentRole;
        private BattleData m_BattleData;

        private bool m_IsAction = false;//指令圆环是否展示

        private BattleOrderType m_OrderType = BattleOrderType.None;
        private IOrderAction m_OrderAction;
        private Action m_OrderFinishCallBack;

        public int m_TempRoleIndex = 0;//记录角色位置的临时数据，之后会替换为下一个角色的位置
        protected internal override void Init()
        {
            base.Init();
            m_BattleData = (BattleData)Game.Data.GetGameData<BattleData>();

            //获取六边形瓦片纵向排列中心点之间距
            m_StandardLength = Mathf.Sqrt((float)Mathf.Pow(2 * GameConst.m_MapTileSize, 2) - (float)Math.Pow(GameConst.m_MapTileSize, 2));
        }

        protected internal override void Destroy()
        {
            base.Destroy();
            m_CurrentTile = null;
            m_CurrentRole = null;
            DestroyInstance();
        }

        public MapTileClass GetCurrenTile()
        {
            return m_CurrentTile;
        }

        public bool RunAction()
        {
            m_IsAction = true;
            return m_IsAction;
        }

        public bool OrderFinish()
        {
            m_OrderType = BattleOrderType.None;
            m_IsAction = false;
            return m_IsAction;
        }

        #region 战斗开始状态操作
        /// <summary>
        /// 进入战斗地图时，将tile生成到对应的位置
        /// </summary>
        /// <param name="_index">tile的ID</param>
        /// <param name="_type">tile的类型</param>
        /// <param name="_pos">tile的位置</param>
        /// <param name="_materialType">tile的材质</param>
        public void SetTileOnInit(int _index, TileType _type, Vector2 _pos, MaterialType _materialType)
        {
            //判断瓦片处于偶数行还是奇数行
            //处于偶数行：瓦片横向坐标 * 2 * 瓦片边长一半
            //处于奇数行：（瓦片横向坐标 * 2 + 1）* 瓦片边长一半
            float _x = _pos[0] % 2 == 0 ? _pos[1] * 2 * GameConst.m_MapTileSize : (_pos[1] * 2 + 1) * GameConst.m_MapTileSize;
            float _y = _pos[0] * m_StandardLength;
            Vector3 _tilePos = new Vector3(_x, 0, _y);

            var kAB = Game.Res.Load(_materialType.ToString(), "material");
            Material kMaterial = Game.Res.GetObject<Material>(kAB, _materialType.ToString());

            GameObject kTiles = MonoBehaviour.Instantiate(m_BattleData.GetTileLoad(_type), _tilePos, Quaternion.identity, Game.Manager.m_GridUnits);
            kTiles.transform.GetChild(0).GetComponent<Renderer>().material = kMaterial;
            kTiles.name = _index.ToString();

            MapTileClass kTile = new MapTileClass();
            kTile.m_ID = _index;
            kTile.m_Type = _type;
            kTile.m_Transform = kTiles.transform;
            m_BattleData.SetTileInstantiateDic(_index, kTile);

            UnityEngine.Debug.Log("已加载瓦片 ID：" + _index);
        }

        /// <summary>
        /// 进入战斗地图时，将角色摆放到初始位置
        /// </summary>
        /// <param name="_index">tile的ID</param>
        public void SetRoleOnInit(int _index)
        {
            //这里会通过角色的UID获取到角色的数据类，根据数据类生成角色
            Game.Res.Load("Role_Cursor_fbx");
            Game.Res.Load("Profession", "ui");

            var kAB = Game.Res.Load("Role_Cursor");
            GameObject kRole = MonoBehaviour.Instantiate(Game.Res.GetObject<GameObject>(kAB, "Role_Cursor"), Game.Manager.m_RoleUnits);

            kRole.transform.localPosition = SetRoleOnUpDate(_index);
            m_TempRoleIndex = _index;
            m_BattleData.m_BattleRoleDic.Add(0, kRole);
            UnityEngine.Debug.Log("生成角色");
        }

        /// <summary>
        /// 将角色摆放到对应位置
        /// </summary>
        /// <param name="_role">角色的游戏对象（后期会修改为角色对应的数据类）</param>
        /// <param name="_tileIndex">tile的ID</param>
        private Vector3 SetRoleOnUpDate(int _tileIndex)
        {
            MapTileClass tile = m_BattleData.GetTileInstantiate(_tileIndex);
            Vector3 kHigh = tile.m_Transform.localPosition;
            switch (tile.m_Type)
            {
                case TileType.normal:
                    kHigh += new Vector3(0, 8f, 0);
                    break;
                case TileType.low:
                    kHigh += new Vector3(0, 4f, 0);
                    break;
                case TileType.high:
                    kHigh += new Vector3(0, 12f, 0);
                    break;
                case TileType.tower:
                    kHigh += new Vector3(0, 50f, 0);
                    break;
                case TileType.wall:
                    kHigh = new Vector3(0, 30f, 0);
                    break;
                default:
                    break;
            }
            return kHigh;
        }
        #endregion

        #region 战斗指令状态操作
        /// <summary>
        /// 判断点击对象并执行相应的操作
        /// </summary>
        /// <param name="_transform"></param>
        public void CheckClick(Transform _transform)
        {
            if (m_IsAction)
            {
                return;
            }
            switch (m_OrderType)
            {
                case BattleOrderType.None:
                    if (_transform.tag == "role")
                    {
                        //BattleOrderRing window = (BattleOrderRing)Game.UI.GetUI(UINameType.Battle_OrderRing);
                        m_CurrentRole = _transform.gameObject;
                        //window.ShowRingUI();
                    }
                    break;
                case BattleOrderType.Retreat:
                    break;
                case BattleOrderType.Attack:
                    break;
                case BattleOrderType.Move:
                    if (_transform.tag == "tile")
                    {
                        m_CurrentTile = m_BattleData.GetTileInstantiate(int.Parse(_transform.name));
                        m_OrderAction.OrderRun(_transform, m_OrderFinishCallBack);
                    }
                    break;
                case BattleOrderType.Skill:
                    break;
                case BattleOrderType.Standby:
                    break;
                case BattleOrderType.Equip:
                    break;
                case BattleOrderType.Item:
                    break;
                default:
                    break;
            }
        }

        public void ChackAction(BattleOrderType _action, Action _callback)
        {
            m_OrderType = _action;
            switch (_action)
            {
                case BattleOrderType.Retreat:
                    m_OrderAction = new ActionRetreat();
                    CheckActionRetreat();
                    break;
                case BattleOrderType.Attack:
                    m_OrderAction = new ActionAttack();
                    CheckActionAttack();
                    break;
                case BattleOrderType.Move:
                    m_OrderAction = new ActionMove();
                    CheckActionMove();
                    break;
                case BattleOrderType.Skill:
                    m_OrderAction = new ActionSkill();
                    CheckActionSkill();
                    break;
                case BattleOrderType.Standby:
                    m_OrderAction = new ActionStandby();
                    CheckActionStandby();
                    break;
                case BattleOrderType.Equip:
                    m_OrderAction = new ActionEquip();
                    CheckActionEquip();
                    break;
                case BattleOrderType.Item:
                    m_OrderAction = new ActionItem();
                    CheckActionItem();
                    break;
                default:
                    m_OrderAction = new ActionStandby();
                    CheckActionStandby();
                    break;
            }
            m_OrderFinishCallBack = _callback;
        }

        public void CheckActionRetreat()
        {
            //执行撤退指令
            UnityEngine.Debug.Log("执行撤退指令");
            //判断撤退概率
            //展示撤退弹窗
            //角色撤退 成功 or 失败
            //结算撤退结果
            //完成指令执行回调
        }

        public void CheckActionAttack()
        {
            //执行攻击指令
            UnityEngine.Debug.Log("执行攻击指令");
            //攻击范围的瓦片亮起
            //选择高亮瓦片中的可攻击目标
            //判断攻击类型 近程 or 远程
            //执行攻击指令
            //结算攻击结果
            //完成指令执行回调
        }

        public void CheckActionMove()
        {
            //执行移动指令
            UnityEngine.Debug.Log("执行移动指令");
            //可移动到的瓦片亮起
            m_OrderAction.OrderIssue();
            //从可移动到的瓦片中选取移动目的地
            //角色移动
            //结算移动结果
            //完成移动回调
        }

        public void CheckActionSkill()
        {
            //执行释放技能指令
            //弹出技能选单（不能释放的技能置灰）
            //选择技能
            //判断技能类型（近程 or 远程）（攻击技能 or 辅助技能）（单体技能 or 范围技能）
            //选择技能释放目标
            //释放技能
            //结算技能结果
            //完成指令执行回调
        }

        public void CheckActionStandby()
        {
            //执行待机指令
            UnityEngine.Debug.Log("执行待机指令");
            //待机语句
            //角色因待机获得的 增益 or 减益
            //结算待机结果
            //完成指令执行回调
        }

        public void CheckActionEquip()
        {
            //执行释放战术装备技能指令
            UnityEngine.Debug.Log("执行释放战术装备技能指令");
            //释放技能语句
            //判断技能是否可以释放
            //判断技能类型（攻击 or 功能）
            //选择技能释放目标
            //释放技能
            //结算技能结果
            //完成指令执行回调
        }

        public void CheckActionItem()
        {
            //执行打开道具栏指令
            UnityEngine.Debug.Log("执行打开道具栏指令");
            //打开道具栏语句
            //选择物品
            //判断物品类型（脱手道具 or 持续道具）
            //使用道具
            //结算道具结果
            //完成指令执行回调
        }
        #endregion

        #region 游戏对象操作
        /// <summary>
        /// 将摄像机移动到对应tile位置（直接移动）
        /// </summary>
        /// <param name="_index">tile的ID</param>
        public void MoveCamera(int _index)
        {
            Vector3 kPos = m_BattleData.GetTileInstantiate(_index).m_Transform.position + new Vector3(0, 55, -50);
            Game.Manager.m_GameCameraTar.localPosition = kPos;
        }

        /// <summary>
        /// 让摄像机跟随目标
        /// </summary>
        /// <param name="_tag">目标类型</param>
        /// <param name="_index">目标ID</param>
        public void CameraFollowTag(int _index, Action _callback)
        {
            Vector3 kPos = m_BattleData.GetTileInstantiate(_index).m_Transform.position + new Vector3(0, 55, -50);
            Game.Manager.m_GameCameraTar.DOLocalMove(kPos, 3, false).OnComplete(() => _callback?.Invoke());
        }

        /// <summary>
        /// 角色移动
        /// </summary>
        /// <param name="_index">tile的ID</param>
        public void MoveRole(int _index, Action _callback = null)
        {
            if (!m_CurrentRole)
            {
                return;
            }
            m_CurrentRole.transform.DOLocalMove(SetRoleOnUpDate(_index), 2, false);
            CameraFollowTag(_index, _callback);
            m_TempRoleIndex = _index;
        }
        #endregion

        public void DestroyInstance()
        {
            for (int i = Game.Manager.m_GridUnits.childCount - 1; i >= 0; i--)
            {
                MonoBehaviour.Destroy(Game.Manager.m_GridUnits.GetChild(i).gameObject);
            }
            for (int i = Game.Manager.m_RoleUnits.childCount - 1; i >= 0; i--)
            {
                MonoBehaviour.Destroy(Game.Manager.m_RoleUnits.GetChild(i).gameObject);
            }
        }
    }
}