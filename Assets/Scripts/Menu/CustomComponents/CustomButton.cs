using System.Collections.Generic;
using Progression;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu.CustomComponents
{
    public class CustomButton : Button
    {
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private List<Graphic> _additionalTargetGraphics = new();

        private AudioClip _defaultClickSound;

        protected override void Awake()
        {
            base.Awake();

            if (_clickSound == null)
                _clickSound = _defaultClickSound;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            PlayClickSound();
        }

        private void PlayClickSound()
        {
            AudioController.Instance.PlaySound(_clickSound);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            Color tintColor = state switch
            {
                SelectionState.Normal => colors.normalColor,
                SelectionState.Highlighted => colors.highlightedColor,
                SelectionState.Pressed => colors.pressedColor,
                SelectionState.Selected => colors.selectedColor,
                SelectionState.Disabled => colors.disabledColor,
                _ => colors.normalColor
            };

            if (transition == Transition.ColorTint)
            {
                Color targetColor = tintColor * colors.colorMultiplier;

                foreach (var graphic in _additionalTargetGraphics)
                {
                    if (graphic == null)
                        continue;

                    graphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
                }
            }
        }

        [ContextMenu("Find All Graphics")]
        public void FindAllGraphics()
        {
            _additionalTargetGraphics.Clear();
            Graphic[] graphics = GetComponentsInChildren<Graphic>();

            foreach (var graphic in graphics)
            {
                _additionalTargetGraphics.Add(graphic);
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            _defaultClickSound = Resources.Load<AudioClip>("Audio/Sounds/Menu/click");
            _clickSound = _defaultClickSound;
        }

        [CustomEditor(typeof(CustomButton))]
        public class CustomButtonEditor : ButtonEditor
        {
            SerializedProperty _clickSound;
            SerializedProperty _additionalTargetGraphics;

            protected override void OnEnable()
            {
                base.OnEnable();
                _clickSound = serializedObject.FindProperty("_clickSound");
                _additionalTargetGraphics = serializedObject.FindProperty("_additionalTargetGraphics");
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI(); // Рисуем стандартный интерфейс Button

                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Button Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_clickSound);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_additionalTargetGraphics, true);

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}