using Framework.Script.Base;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.Operate;

namespace Framework.Script.Manager
{
    public class OperateManager : Singleton<OperateManager>
    {
        private Dictionary<string, GameObject> m_OperateDic = new Dictionary<string, GameObject>();
        private GameObject m_CurrentOperate;

        protected override void OnInit()
        {
            GetAllOperate();
        }

        public void GetAllOperate()
        {
            foreach (var element in Game.Data.GetData.operate.DataList)
            {
                string kname = element.Name;
                if (string.IsNullOrEmpty(kname)) continue;
                var kAB = Game.Res.Load(kname);
                GameObject operate = Game.Res.GetObject<GameObject>(kAB, kname);
                Debug.Log("操作逻辑已加载    OperateName：" + kAB.name);
                m_OperateDic.Add(kname, operate);
            }
        }

        public GameOperate LoadOperate(string _name)
        {
            DestroyOperate();
            GameObject obj = MonoBehaviour.Instantiate<GameObject>(m_OperateDic[_name], Game.Manager.m_Dontdestroy);
            m_CurrentOperate = obj;
            return obj.GetComponent<GameOperate>();
        }

        public void DestroyOperate()
        {
            MonoBehaviour.Destroy(m_CurrentOperate);
        }
    }
}