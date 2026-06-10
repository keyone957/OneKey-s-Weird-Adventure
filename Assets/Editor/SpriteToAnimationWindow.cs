using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace ProjectAdventure.Editor
{
    public class SpriteToAnimationWindow : EditorWindow
    {
        private string _clipName = "NewAnimation";
        private string _saveFolderPath = "Assets/08.Animations";
        private float _frameRate = 12f;
        private bool _loopTime = true;
        private List<Sprite> _selectedSprites = new List<Sprite>();

        [MenuItem("Tools/Sprite To Animation Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteToAnimationWindow>("Sprite to Anim");
            window.minSize = new Vector2(350, 450);
            window.Show();
        }

        private void OnEnable()
        {
            // 프로젝트 뷰에서 선택된 스프라이트들을 자동으로 가져옴
            UpdateSelection();
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            UpdateSelection();
            Repaint();
        }

        private void UpdateSelection()
        {
            _selectedSprites.Clear();
            var selectedObjects = Selection.objects;

            // 선택된 객체들 중 스프라이트만 필터링하여 프레임 배열 구축
            foreach (var obj in selectedObjects)
            {
                if (obj is Sprite sprite)
                {
                    _selectedSprites.Add(sprite);
                }
                else if (obj is Texture2D tex)
                {
                    // 텍스처 자체를 선택한 경우 내부에 슬라이스된 스프라이트들을 다 로드
                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (var subAsset in assets)
                    {
                        if (subAsset is Sprite subSprite)
                        {
                            _selectedSprites.Add(subSprite);
                        }
                    }
                }
            }

            // 이름 순으로 정렬하여 프레임이 뒤섞이는 것 방지
            _selectedSprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        }

        private void OnGUI()
        {
            GUILayout.Label("🎨 Sprite to Animation Clip Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _clipName = EditorGUILayout.TextField("Animation Clip Name", _clipName);
            _saveFolderPath = EditorGUILayout.TextField("Save Folder Path", _saveFolderPath);
            _frameRate = EditorGUILayout.FloatField("Frame Rate (FPS)", _frameRate);
            _loopTime = EditorGUILayout.Toggle("Loop Time (반복 재생)", _loopTime);

            EditorGUILayout.Space();
            GUILayout.Label($"선택된 스프라이트 프레임 수: {_selectedSprites.Count}", EditorStyles.miniLabel);

            // 스크롤 뷰로 선택된 스프라이트 목록 보여주기
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (_selectedSprites.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(_selectedSprites.Count, 10); i++)
                {
                    EditorGUILayout.LabelField($"Frame {i}: {_selectedSprites[i].name}");
                }
                if (_selectedSprites.Count > 10)
                {
                    EditorGUILayout.LabelField($"... 외 {_selectedSprites.Count - 10}개 프레임 더 있음");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("프로젝트 뷰(Assets)에서 애니메이션으로 만들 스프라이트들을 드래그/다중 선택해 주세요.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            GUI.enabled = _selectedSprites.Count > 0 && !string.IsNullOrEmpty(_clipName);
            if (GUILayout.Button("🎬 Animation Clip 생성하기", GUILayout.Height(40)))
            {
                GenerateAnimationClip();
            }
            GUI.enabled = true;
        }

        private void GenerateAnimationClip()
        {
            // 폴더 확인 및 생성
            if (!Directory.Exists(_saveFolderPath))
            {
                Directory.CreateDirectory(_saveFolderPath);
            }

            // 애니메이션 클립 생성
            AnimationClip clip = new AnimationClip();
            clip.frameRate = _frameRate;

            // 2D Sprite 키프레임 바인딩 설정
            EditorCurveBinding curveBinding = new EditorCurveBinding();
            curveBinding.type = typeof(SpriteRenderer);
            curveBinding.path = ""; // 루트 오브젝트의 SpriteRenderer를 타겟으로 함
            curveBinding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[_selectedSprites.Count];
            float frameTime = 1f / _frameRate;

            for (int i = 0; i < _selectedSprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].time = i * frameTime;
                keyframes[i].value = _selectedSprites[i];
            }

            AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);

            // 반복 재생 세팅
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = _loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // 파일 저장
            string finalPath = Path.Combine(_saveFolderPath, _clipName + ".anim");
            
            // 기존 파일 덮어쓰기 방어
            finalPath = AssetDatabase.GenerateUniqueAssetPath(finalPath);
            AssetDatabase.CreateAsset(clip, finalPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Generator] ✅ 애니메이션 클립이 성공적으로 생성되었습니다: {finalPath}");
            EditorUtility.DisplayDialog("생성 완료", $"애니메이션 클립 생성 완료!\n경로: {finalPath}", "OK");
        }
    }
}
