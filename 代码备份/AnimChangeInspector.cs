using Framework.Script.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Anim_Change))]
public class AnimChangeInspector : Editor
{
    Anim_Change Anim;

    public void OnEnable()
    {
        Anim = (Anim_Change)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("基础参数");

        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("动画持续时间(秒)", GUILayout.MaxWidth(100));
        Anim.m_Speed = EditorGUILayout.FloatField(Anim.m_Speed);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("动画延迟(毫秒)", GUILayout.MaxWidth(100));
        Anim.m_Delay = EditorGUILayout.IntField(Anim.m_Delay);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("自动播放", GUILayout.MaxWidth(100));
        Anim.m_IsStartShow = EditorGUILayout.Toggle(Anim.m_IsStartShow);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("动画状态");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("状态1状态参数", GUILayout.MaxWidth(90));
        Anim.m_State_1_alpha = EditorGUILayout.FloatField(Anim.m_State_1_alpha);
        EditorGUILayout.LabelField("状态1活跃状态", GUILayout.MaxWidth(90));
        Anim.m_State_1_touch = EditorGUILayout.Toggle(Anim.m_State_1_touch);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("状态2状态参数", GUILayout.MaxWidth(90));
        Anim.m_State_2_alpha = EditorGUILayout.FloatField(Anim.m_State_2_alpha);
        EditorGUILayout.LabelField("状态2活跃状态", GUILayout.MaxWidth(90));
        Anim.m_State_2_touch = EditorGUILayout.Toggle(Anim.m_State_2_touch);
        EditorGUILayout.EndHorizontal();
    }
}