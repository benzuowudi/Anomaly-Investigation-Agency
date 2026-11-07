using Framework.Script.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Anim_Reveal))]
public class AnimRevealInspector : Editor
{
    Anim_Reveal Anim;
    public void OnEnable()
    {
        Anim = (Anim_Reveal)target;
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

        EditorGUILayout.LabelField("动画参数");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("初始透明度", GUILayout.MaxWidth(90));
        Anim.m_Start = EditorGUILayout.FloatField(Anim.m_Start);
        EditorGUILayout.LabelField("目标透明度", GUILayout.MaxWidth(90));
        Anim.m_End = EditorGUILayout.FloatField(Anim.m_End);
        EditorGUILayout.EndHorizontal();
    }
}
