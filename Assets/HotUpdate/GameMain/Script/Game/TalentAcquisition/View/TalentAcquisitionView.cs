using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Data;
using GameMain.Script.Role;
using GameMain.Script.TalentAcquisition;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 人才招募界面（FairyGUI 包 TalentAcquisition / TalentAcquisition）。
    ///
    /// 左侧 TACom：人才吸纳，list_com 填 TADescCom（合同说明）
    /// 右侧 TACom：定向猎头，list_com 先填 TADescCom（猎头说明）再填 TAConditionCom（猎头需求）
    /// </summary>
    public class TalentAcquisitionView : UIWindow
    {
        /// <summary>猎头说明（设计稿示意文案，等策划配置表接入后替换）</summary>
        private const string c_HunterDesc =
            "1，花费【10克黄金】购置一张【定向猎头委托】；\n" +
            "2，消费【定向猎头委托】需要设置委托需求；\n" +
            "3，猎头会按照委托需求行动，并在一定时间后带回人才；";

        private GButton btn_back;
        private GTextField txt_title_1, txt_title_2;
        private TACom com_TA_1, com_TA_2;
        private TalentAcquisitionData m_Data;
        private int m_LastRemaining = -1;

        protected internal override void Init(object o)
        {
            btn_back = ui.GetChild("btn_back").asButton;
            txt_title_1 = ui.GetChild("txt_title_1").asTextField;
            txt_title_2 = ui.GetChild("txt_title_2").asTextField;
            com_TA_1 = ui.GetChild("com_TA_1") as TACom;
            com_TA_2 = ui.GetChild("com_TA_2") as TACom;
            m_Data = Game.Data.GetGameData<TalentAcquisitionData>();

            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            btn_back.onClick.Add(OnClickBack);
            com_TA_1.onClickOnce.Add(OnClickOnce);
            com_TA_1.onClickTen.Add(OnClickTen);
            com_TA_2.onClickHeadhunter.Add(OnClickHunter);
        }

        protected internal override void RemoveEvent()
        {
            btn_back.onClick.Remove(OnClickBack);
            com_TA_1.onClickOnce.Remove(OnClickOnce);
            com_TA_1.onClickTen.Remove(OnClickTen);
            com_TA_2.onClickHeadhunter.Remove(OnClickHunter);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            Refresh();
            RefreshHeadhunterTime();

            // 上线后点开人才招募时，如果猎头已经结束，直接结算并展示结果
            if (m_Data.GetHeadhunterEndTime() > 0 && !m_Data.IsHeadhunterActive())
            {
                CompleteHeadhunter();
            }
        }

        private void Update()
        {
            if (m_Data == null)
            {
                return;
            }

            if (!m_Data.IsHeadhunterActive())
            {
                if (m_Data.GetHeadhunterEndTime() > 0)
                {
                    CompleteHeadhunter();
                    return;
                }
                if (m_LastRemaining > 0)
                {
                    com_TA_2.SetHeadhunterTime("00:00:00");
                    com_TA_2.SetHeadhunterRunning(false);
                    m_LastRemaining = 0;
                }
                return;
            }

            int kRemaining = m_Data.GetHeadhunterRemainingSeconds();
            if (kRemaining != m_LastRemaining)
            {
                com_TA_2.SetHeadhunterTime(TACom.FormatTime(kRemaining));
                m_LastRemaining = kRemaining;
            }
        }

        private void Refresh()
        {
            txt_title_1.text = "人才招募";
            txt_title_2.text = "TALENT ACQUISTION";

            com_TA_1.SetState(TACom.TAState.Recruit);
            com_TA_1.SetTitle("人才吸纳");
            com_TA_1.SetConsume("ui://CommonUI/ui_icon_euro_dark",
                "200/" + m_Data.GetEuro(), "2000/" + m_Data.GetEuro());
            com_TA_1.SetDesc("合同说明：");
            com_TA_1.SetButtons("发送合同", 1, "发送合同", 10);

            com_TA_2.SetState(TACom.TAState.Headhunter);
            List<string> kConditionTexts = new List<string>();
            List<int> kConditionTimes = new List<int>();
            GetConditions(kConditionTexts, kConditionTimes);

            List<string> kSelectedConditions = m_Data.GetHeadhunterEndTime() > 0
                ? m_Data.GetHeadhunterConditions()
                : null;

            com_TA_2.SetTitle("定向猎头");
            com_TA_2.SetConsume("ui://CommonUI/ui_icon_glod", "10/" + m_Data.GetGold(), "0/0");
            com_TA_2.SetDescAndConditions("猎头说明：", c_HunterDesc, "猎头需求：",
                kConditionTexts, kConditionTimes, kSelectedConditions);
            com_TA_2.SetHeadhunterButton("猎头委托", "准备就绪");
        }

        private static void GetConditions(List<string> _texts, List<int> _times)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables != null && kTables.talentAcquisition != null)
            {
                List<cfg.GameData_Role.talentAcquisitionData> kRows = kTables.talentAcquisition.DataList;
                for (int i = 0; i < kRows.Count; i++)
                {
                    _texts.Add(kRows[i].Desc);
                    _times.Add(kRows[i].Time);
                }
            }
        }

        private void OnClickOnce()
        {
            TalentAcquisitionResult kResult = TalentAcquisitionSystem.Recruit(1);
            if (kResult != null)
            {
                Refresh();
                Game.UI.ShowWindow(UIDefine.TalentAcquisitionResult, null, kResult, true);
            }
        }

        private void OnClickTen()
        {
            TalentAcquisitionResult kResult = TalentAcquisitionSystem.Recruit(10);
            if (kResult != null)
            {
                Refresh();
                Game.UI.ShowWindow(UIDefine.TalentAcquisitionResult, null, kResult, true);
            }
        }

        private void OnClickHunter()
        {
            int kSeconds = com_TA_2.GetSelectedHeadhunterTime();
            if (kSeconds <= 0)
            {
                Game.Log.LogWarning("[人才招募] 请先选择猎头需求");
                return;
            }
            if (m_Data.IsHeadhunterActive())
            {
                Game.Log.LogWarning("[人才招募] 已有猎头正在进行中");
                return;
            }

            List<string> kConditions = com_TA_2.GetSelectedConditions();
            if (!TalentAcquisitionSystem.StartHeadhunter(kSeconds, kConditions))
            {
                return;
            }

            Refresh();
            RefreshHeadhunterTime();
        }

        private void CompleteHeadhunter()
        {
            RoleRecord kRole = TalentAcquisitionSystem.CompleteHeadhunter();
            com_TA_2.SetHeadhunterRunning(false);
            if (kRole == null)
            {
                return;
            }

            TalentAcquisitionResult kResult = new TalentAcquisitionResult();
            kResult.m_IsTen = false;
            kResult.m_IsDisplayOnly = true;
            kResult.m_Candidates.Add(kRole);
            Refresh();
            Game.UI.ShowWindow(UIDefine.TalentAcquisitionResult, null, kResult, true);
        }

        private void RefreshHeadhunterTime()
        {
            if (m_Data.IsHeadhunterActive())
            {
                com_TA_2.SetHeadhunterRunning(true);
                int kRemaining = m_Data.GetHeadhunterRemainingSeconds();
                com_TA_2.SetHeadhunterTime(TACom.FormatTime(kRemaining));
                m_LastRemaining = kRemaining;
            }
            else if (m_Data.GetHeadhunterEndTime() > 0)
            {
                com_TA_2.SetHeadhunterRunning(false);
                com_TA_2.SetHeadhunterTime("00:00:00");
                m_LastRemaining = 0;
            }
            else
            {
                com_TA_2.SetHeadhunterRunning(false);
                com_TA_2.SetHeadhunterTime("--:--:--");
                m_LastRemaining = -1;
            }
        }

        private void OnClickBack()
        {
            Game.UI.ShowWindow(UIDefine.CommandRoom);
        }
    }
}
