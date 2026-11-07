using cfg.GameType;
using Framework.Script.Data;
using GameMain.Script.Class;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMain.Script.Data
{
    public class BattleData : GameData
    {
        /// <summary>
        /// 预制的瓦片实例
        /// </summary>
        public readonly Dictionary<TileType, GameObject> m_TileLoadDic = new Dictionary<TileType, GameObject>();
        /// <summary>
        /// 生成的瓦片实例
        /// </summary>
        public readonly Dictionary<int, MapTileClass> m_TileInstantiateDic = new Dictionary<int, MapTileClass>();
        /// <summary>
        /// 生成在战斗场景中的角色
        /// </summary>
        public readonly Dictionary<int, GameObject> m_BattleRoleDic = new Dictionary<int, GameObject>();
        protected internal override void Init()
        {
            base.Init();
            LoadTiles();
        }

        protected internal override void Update(object o)
        {
            base.Update(o);
        }

        protected internal override void Destroy()
        {
            base.Destroy();
            m_BattleRoleDic.Clear();
            m_TileInstantiateDic.Clear();
        }

        /// <summary>
        /// 获取并存储全部的预制瓦片实例
        /// </summary>
        private void LoadTiles()
        {
            foreach (TileType _type in Enum.GetValues(typeof(TileType)))
            {
                if (!CheckTileLoadByKey(_type))
                {
                    string kABName = "MapTile_" + _type.ToString();
                    Game.Res.Load(kABName + "_fbx");
                    var kAB = Game.Res.Load(kABName);
                    GameObject kTiles = Game.Res.GetObject<GameObject>(kAB, kABName);
                    kTiles.transform.localPosition = Vector3.zero;
                    kTiles.transform.localEulerAngles = Vector3.zero;
                    kTiles.transform.localScale = Vector3.one;
                    m_TileLoadDic.Add(_type, kTiles);
                }
                else
                {
                    continue;
                }
            }
        }

        public void SetTileInstantiateDic(int _key, MapTileClass _value)
        {
            m_TileInstantiateDic.Add(_key, _value);
        }

        public GameObject GetTileLoad(TileType _key)
        {
            GameObject obj = null;
            m_TileLoadDic.TryGetValue(_key, out obj);
            return obj;
        }

        public MapTileClass GetTileInstantiate(int _key)
        {
            MapTileClass _tile = null;
            m_TileInstantiateDic.TryGetValue(_key, out _tile);
            return _tile;
        }

        public bool CheckTileLoadByKey(TileType _key)
        {
            if (!m_TileLoadDic.ContainsKey(_key)) { return false; }
            return true;
        }

        public bool CheckTileInstantiateByKey(int _key)
        {
            if (!m_TileInstantiateDic.ContainsKey(_key)) { return false; }
            return true;
        }
    }
}