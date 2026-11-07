using cfg;
using Framework.Script.Base;
using Framework.Script.Constant;
using Framework.Script.Data;
using GameMain.Script.Class;
using GameMain.Script.Data;
using GameMain.Script.Role;
using GameMain.Script.Warehouse;
using Nino.Serialization;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Framework.Script.Manager
{
    public class DataManager : Singleton<DataManager>
    {
        private Tables m_Tables;
        private Tables m_PersistenceTable;

        private Dictionary<Type, GameData> m_GameDataDic = new Dictionary<Type, GameData>();


        protected override void OnInit()
        {
            CreatPersistencePath();
            m_Tables = new Tables(CacheDataLoader);
            LoadGameData();
        }

        public JSONNode CacheDataLoader(string _fileName)
        {
            return JSON.Parse(File.ReadAllText(string.Format(GameConst.m_DataJsonPath, _fileName)));
        }

        /// <summary>
        ///获取缓存数据
        /// </summary>
        public Tables GetData
        {
            get { return m_Tables; }
        }

        /// <summary>
        /// 创建持久化数据json
        /// </summary>
        public void CreatJson()
        {
        }

        public void CreatPersistencePath()
        {
            if (!Directory.Exists(Application.persistentDataPath + "/GameMain/Data"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/GameMain/Data");
            }
        }

        /// <summary>
        /// 在本地保存持久化数据
        /// </summary>
        public void SaveData<T>(object o, string _name) where T : IGameClass
        {
            byte[] ninoBuffer = Array.Empty<byte>();
            ninoBuffer = Serializer.Serialize<T>((T)o);

            FileStream fs = new FileStream(string.Format(GameConst.m_PersistenceDataPath, _name), FileMode.Create);
            fs.Write(ninoBuffer, 0, ninoBuffer.Length);
            fs.Dispose();
        }

        /// <summary>
        /// 获取持久化数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPersistenceData<T>(string _name) where T : IGameClass
        {
            Debug.Log(string.Format(GameConst.m_PersistenceDataPath, _name));
            byte[] kByte = FileContent(string.Format(GameConst.m_PersistenceDataPath, _name));
            var kData = Nino.Serialization.Deserializer.Deserialize<T>(kByte);
            return kData;
        }

        private byte[] FileContent(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    byte[] buffur = new byte[fs.Length];
                    fs.Read(buffur, 0, (int)fs.Length);
                    return buffur;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void LoadGameData()
        {
            // RegisterData<BattleData>();
            // RegisterData<FightProcessData>();
            RegisterData<BaseData>();
            RegisterData<CommandRoomData>();
            RegisterData<RoleData>();
            RegisterData<RoleSystemData>();
            RegisterData<TalentAcquisitionData>();
            RegisterData<WarehouseData>();
            // 读档放在最后：此时所有 GameData 都已注册，事件监听方也能收到 OnRolesChanged
            RoleSystem.Load();
        }

        public void RegisterData<T>() where T : GameData, new()
        {
            T kData = new T();
            m_GameDataDic.Add(typeof(T), kData);
            m_GameDataDic[typeof(T)].Init();
        }

        public T GetGameData<T>(object o = null) where T : GameData
        {
            if (!m_GameDataDic.ContainsKey(typeof(T)))
            {
                Debug.LogError("未找到GameData实例：" + typeof(T).ToString());
            }
            m_GameDataDic[typeof(T)].Update(o);
            return (T)m_GameDataDic[typeof(T)];
        }

        public void DestroyData<T>() where T : GameData
        {
            if (!m_GameDataDic.ContainsKey(typeof(T)))
            {
                Debug.Log("未找到GameData实例：" + typeof(T).ToString());
            }
            m_GameDataDic[typeof(T)].Destroy();
        }

        #region 获取XMLNode的方法（暂时用不到，数据读取全部转移到LuBan，数据存储转移到Nino）
        [Obsolete]
        public System.Xml.XmlNodeList GetXmlData(string _dataName)
        {
            var kAB = Game.Res.Load(_dataName, _suffix: "data");
            var kTextAsset = Game.Res.GetObject<TextAsset>(kAB, _dataName);
            System.Xml.XmlDocument kXmlDoc = new System.Xml.XmlDocument();
            kXmlDoc.LoadXml(kTextAsset.ToString());
            System.Xml.XmlNodeList list = kXmlDoc.SelectSingleNode("data").ChildNodes;
            return list;
        }
        #endregion
        #region 获取数据条目数量，已弃用，JSON读取全部转移到SimpleJSON
        [Obsolete]
        public int GetDataCount(string _name)
        {
            var kAB = Game.Res.Load(_name);
            var kText = Game.Res.GetObject<TextAsset>(kAB, "data");
            LitJson.JsonData kJsonData = LitJson.JsonMapper.ToObject(kText.text);
            return kJsonData[_name].Count;
        }
        #endregion
    }
}