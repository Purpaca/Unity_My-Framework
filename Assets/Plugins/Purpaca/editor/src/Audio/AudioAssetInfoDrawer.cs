using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Purpaca.Editor.Audio
{
    [CustomPropertyDrawer(typeof(ManagedAudio.AudioAssetInfo))]
    public class AudioAssetInfoDrawer : PropertyDrawer
    {
        private readonly Type[] _types = { typeof(AudioClip), typeof(AudioSequence) };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty asset = property.FindPropertyRelative("m_asset");
            SerializedProperty typeProp = property.FindPropertyRelative("m_type");

            if (GUILayout.Button("xuanze")) 
            {
                OpenCustomSelector(asset);
            }

            // 首行布局
            Rect fieldRect = EditorGUI.PrefixLabel(position, label);

            // 绘制对象字段（允许拖拽）
            EditorGUI.BeginChangeCheck();
            Object newObj = EditorGUI.ObjectField(
                position: fieldRect,
                obj: asset.objectReferenceValue,
                objType: typeof(Object),
                allowSceneObjects: false
            );

            // 类型验证
            if (EditorGUI.EndChangeCheck())
            {
                if (IsValidAsset(newObj))
                {
                    asset.objectReferenceValue = newObj;
                    typeProp.enumValueIndex = GetAssetTypeAsIndex(newObj);
                }
                else
                {
                    //Debug.LogWarning("仅支持AudioClip和SequenceSO资源");
                }
            }

            /*
            // 第二行：显示类型信息和额外操作
            if (asset.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;

                // 显示当前类型
                EditorGUILayout.LabelField("当前类型", typeProp.enumDisplayNames[typeProp.enumValueIndex]);

                EditorGUI.indentLevel--;
            }
            */

            if (asset.objectReferenceValue == null)
            {
                // 绘制提示文本
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                };

                EditorGUILayout.LabelField("拖放AudioClip或Sequence至此", style);
            }

            



            EditorGUI.EndProperty();
        }

        private bool IsValidAsset(Object obj)
        {
            return obj is AudioClip || obj is AudioSequence || obj == null;
        }

        private int GetAssetTypeAsIndex(Object obj)
        {
            int value = -1;
            if (obj == null)
            {
                value = 0;
            }
            else if (obj is AudioClip)
            {
                value = 1;
            }
            else if (obj is AudioSequence)
            {
                value = 2;
            }

            return value;
        }


        private void OpenCustomSelector(SerializedProperty property)
        {
            EditorApplication.delayCall += () =>
            {
                var selector = ScriptableObject.CreateInstance<FilteredSelector>();
                selector.Setup(
                    title: "选择音频/序列",
                    types: _types,
                    onSelect: selected =>
                    {
                        property.objectReferenceValue = selected;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                );
                selector.ShowUtility();
            };
        }

        private class FilteredSelector : EditorWindow
        {
            private Type[] _filterTypes;
            private Action<Object> _callback;

            public void Setup(string title, Type[] types, Action<Object> onSelect)
            {
                titleContent = new GUIContent(title);
                _filterTypes = types;
                _callback = onSelect;
            }

            void OnGUI()
            {
                // 使用SearchableEditorWindow的特性实现资源搜索
                foreach (var asset in FindAssetsByType(_filterTypes))
                {
                    if (GUILayout.Button(asset.name, EditorStyles.miniButton))
                    {
                        _callback?.Invoke(asset);
                        Close();
                    }
                }
            }

            private static IEnumerable<Object> FindAssetsByType(Type[] types)
            {
                foreach (Type t in types)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{t.Name}");
                    foreach (string guid in guids)
                    {
                        yield return AssetDatabase.LoadAssetAtPath<Object>(
                            AssetDatabase.GUIDToAssetPath(guid));
                    }
                }
            }
        }
    }
}
