using System.Collections.Generic;
using UnityEngine;
using Framework.Script.Data;
using cfg.GameData_Story;
using GameMain.Script.Class;
using cfg.GameType;

namespace GameMain.Script.Data
{
    public class FightProcessData : GameData
    {
        public BattleClass m_BattleClass { get; private set; }
        public MainStoryData m_MainStory { get; private set; }
        private Dictionary<int, BattleClass> m_BattleClassDic = new Dictionary<int, BattleClass>();
        private Dictionary<int, MainStoryData_Group> m_GroupDic = new Dictionary<int, MainStoryData_Group>();
        private Dictionary<int, MainStoryData_Chapter> m_ChapterDic = new Dictionary<int, MainStoryData_Chapter>();
        private Dictionary<int, MainStoryData_Level> ma_LevelDic = new Dictionary<int, MainStoryData_Level>();
        public Dictionary<int, GroupData> m_GroupData { get; private set; }
        public Dictionary<int, ChapterData> m_ChapterData { get; private set; }

        protected internal override void Init()
        {
            base.Init();
            //获取到章节持久化数据
            m_MainStory = new MainStoryData();
            m_GroupData = Game.Data.GetData.group.DataMap;
            m_ChapterData = Game.Data.GetData.chapter.DataMap;
        }

        protected internal override void Update(object o)
        {
            base.Update(o);
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private void LoadPersistenceData()
        {
        }

        public void SetBattleClass(int _battleIndex = 0, int _goupIndex = 0, int _chapterIndex = 0, int _levelIndex = 0, WeatherType _weatherType = WeatherType.sunny,
            int _mapIndex = 0, Dictionary<int, BattleRoleClass> _battleRoleClassDic = null)
        {
            if (m_BattleClassDic.ContainsKey(_battleIndex))
            {
                m_BattleClass = m_BattleClassDic[_battleIndex];
            }
            else
            {
                m_BattleClass = new BattleClass();
            }
            m_BattleClass.m_BattleID = _battleIndex;
            m_BattleClass.m_Group = _goupIndex;
            m_BattleClass.m_Chapter = _chapterIndex;
            m_BattleClass.m_Level = _levelIndex;
            m_BattleClass.m_WeatherType = _weatherType;
            m_BattleClass.m_Map = _mapIndex;
            m_BattleClass.m_BattleRoleClassDic = _battleRoleClassDic;

            m_BattleClassDic.Add(_battleIndex, m_BattleClass);
        }

        public BattleClass GetBattleByindex(int _battleIndex)
        {
            if (m_BattleClassDic.ContainsKey(_battleIndex))
            {
                return m_BattleClassDic[_battleIndex];
            }
            Debug.LogError("无此战斗数据BattleID = " + _battleIndex);
            return null;
        }
    }
}