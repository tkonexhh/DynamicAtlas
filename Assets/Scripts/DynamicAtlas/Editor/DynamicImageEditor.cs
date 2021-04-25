using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

namespace GFrame
{
    [CustomEditor(typeof(DynamicImage))]
    public class DynamicImageEditor : ImageEditor
    {
        private DynamicImage m_Target;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Target = (DynamicImage)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(5);
            EditorGUILayout.LabelField("--------------------------------------------------------------------------------------------------------------------");
            m_Target.atlasGroup = (DynamicAtlasGroup)EditorGUILayout.EnumPopup("Group", m_Target.atlasGroup);
        }
    }
}