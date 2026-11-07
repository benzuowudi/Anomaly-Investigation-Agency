using System;
using FairyGUI;
using Framework.Script.UI;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 常驻背景层（预制体 BackGroundView，内容包 Base / BackGroundVIew）。
    /// 由 Game.CreateUI 在 B_Layer 上显示一次，之后一直留在最底层，业务界面都画在它上面。
    ///
    /// 结构：
    ///   n0          全屏背景图 ui_bg_mainBg
    ///   part_topTip 左上角装饰线 + 品牌名 txt_name（设计稿固定文案，脚本不覆盖）
    ///   txt_date    右下日期，每秒刷新一次
    ///   txt_verson  右下版本号，取 ProjectSettings 的 bundleVersion
    /// </summary>
    public class BackGroundUI : UIWindow
    {
        private GTextField txt_date;
        private GTextField txt_verson;

        private int m_LastSecond = -1;

        protected internal override void Init(object o)
        {
            txt_date = ui.GetChild("txt_date").asTextField;
            txt_verson = ui.GetChild("txt_verson").asTextField;

            base.Init(o);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            txt_verson.text = "V " + Application.version;
            RefreshDate(DateTime.Now);
        }

        private void Update()
        {
            DateTime kNow = DateTime.Now;
            if (kNow.Second == m_LastSecond)
            {
                return;
            }

            RefreshDate(kNow);
        }

        private void RefreshDate(DateTime _now)
        {
            m_LastSecond = _now.Second;
            txt_date.text = _now.ToString("yy/MM/dd HH:mm:ss");
        }
    }
}
