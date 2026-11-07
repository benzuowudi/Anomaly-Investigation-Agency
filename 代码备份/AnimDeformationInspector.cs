using Framework.Script.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Anim_Deformation))]
public class AnimDeformationInspector : Editor
{
    Anim_Deformation Anim;
    public void OnEnable()
    {
        Anim = (Anim_Deformation)target;
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
        EditorGUILayout.BeginVertical();
        Anim.m_Start = EditorGUILayout.Vector2Field("初始尺寸", Anim.m_Start);
        Anim.m_End = EditorGUILayout.Vector2Field("目标尺寸", Anim.m_End);
        EditorGUILayout.EndVertical();
    }
}
