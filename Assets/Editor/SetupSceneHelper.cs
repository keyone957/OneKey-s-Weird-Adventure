using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using Unity.Cinemachine;
using System.IO;

namespace ProjectAdventure.Editor
{
    public static class SetupSceneHelper
    {
        private const string LuchadorSpritesFolder = "Assets/Luchador Asset Pack by TikiTed/Sprites";
        private const string LuchadorAnimFolder = "Assets/Luchador Asset Pack by TikiTed/Animations";

        [MenuItem("Tools/Setup Player and Camera")]
        public static void SetupPlayerAndCamera()
        {
            // 1. TestScene 열기
            string targetScenePath = "Assets/00.Test/TestScene.unity";
            var activeScene = EditorSceneManager.GetActiveScene();

            if (activeScene.path != targetScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                activeScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            }

            // 2. 에셋 팩 스프라이트 임포터 강제 보정 (Point Filter, 16 PPU)
            string[] sheetFiles = {
                "luchador-walk-down.png",
                "luchador-walk-down-left.png",
                "luchador-walk-left.png",
                "luchador-walk-up-left.png",
                "luchador-walk-up.png",
                "luchador-walk-up-right.png",
                "luchador-walk-right.png",
                "luchador-walk-down-right.png",
                "luchador-idle-right.png"
            };

            foreach (string file in sheetFiles)
            {
                string path = $"{LuchadorSpritesFolder}/{file}";
                if (!File.Exists(path)) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsToUnits = 16;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();

            // 3. 종합 Animator Controller 자동 빌드
            string controllerPath = $"{LuchadorAnimFolder}/Luchador_Controller.controller";
            AnimatorController controller = null;

            if (File.Exists(controllerPath))
            {
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            }

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // 기존 상태 머신 클리어 후 재생성
            var rootStateMachine = controller.layers[0].stateMachine;
            var states = rootStateMachine.states;
            for (int i = states.Length - 1; i >= 0; i--)
            {
                rootStateMachine.RemoveState(states[i].state);
            }

            // 모든 에셋 애니메이션 클립 매핑 등록
            string[] clipNames = {
                "Idle", "Walk_Down", "Walk_Down_Left", "Walk_Down_Right",
                "Walk_Left", "Walk_Right", "Walk_Up", "Walk_Up_Left", "Walk_Up_Right",
                "Attack_1", "Attack_2", "Attack_3", "Death", "Hit", "Look", "Run"
            };

            foreach (string clipName in clipNames)
            {
                string clipPath = $"{LuchadorAnimFolder}/{clipName}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip != null)
                {
                    var state = rootStateMachine.AddState(clipName);
                    state.motion = clip;
                    // 디폴트 대기 상태로 설정
                    if (clipName == "Idle")
                    {
                        rootStateMachine.defaultState = state;
                    }
                }
                else
                {
                    Debug.LogWarning($"[Setup] 애니메이션 클립을 찾을 수 없습니다: {clipPath}");
                }
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // 4. 스폰 높이 감지
            Vector3 spawnPos = Vector3.zero;
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float h = terrain.SampleHeight(Vector3.zero) + terrain.transform.position.y;
                spawnPos = new Vector3(0f, h + 0.1f, 0f);
            }
            else
            {
                if (Physics.Raycast(new Vector3(0, 100, 0), Vector3.down, out RaycastHit hit, 200f))
                    spawnPos = hit.point + Vector3.up * 0.1f;
                else
                    spawnPos = new Vector3(0, 1, 0);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Luchador Player with Animator");

            // 5. Player 오브젝트 생성/탐색
            var playerObj = GameObject.Find("Player") ?? new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            playerObj.transform.position = spawnPos;

            // CharacterController 안전한 널체크 및 할당 (유니티 Fake Null 방지)
            var cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = playerObj.AddComponent<CharacterController>();
            }
            cc.center = new Vector3(0, 1f, 0); 
            cc.radius = 0.4f; 
            cc.height = 1.8f;

            // PlayerController 안전한 널체크 및 할당
            var pc = playerObj.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = playerObj.AddComponent<PlayerController>();
            }

            // 6. Visual 자식 오브젝트 생성/탐색
            var visualT = playerObj.transform.Find("Visual");
            GameObject visual;
            if (visualT == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(playerObj.transform);
                visual.transform.localPosition = new Vector3(0, 0.9f, 0);
                visual.transform.localScale = new Vector3(3f, 3f, 3f);
                Undo.RegisterCreatedObjectUndo(visual, "Create Visual");
            }
            else
            {
                visual = visualT.gameObject;
                visual.transform.localScale = new Vector3(3f, 3f, 3f);
            }

            // Animator 컴포넌트 재사용 및 바인딩 (유니티 Fake Null 방지)
            var anim = visual.GetComponent<Animator>();
            if (anim == null)
            {
                anim = visual.AddComponent<Animator>();
            }
            anim.runtimeAnimatorController = controller;

            // SpriteRenderer 세팅
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = visual.AddComponent<SpriteRenderer>();
            }
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            sr.receiveShadows = true;

            // 기본 정면 스프라이트 강제 세팅 (최초 비주얼용)
            Sprite[] spritesS = LoadSpritesFromSheet($"{LuchadorSpritesFolder}/luchador-walk-down.png");
            if (spritesS != null && spritesS.Length > 0)
            {
                sr.sprite = spritesS[0];
            }

            // 빌보드 컴포넌트
            if (visual.GetComponent<Billboard>() == null)
            {
                visual.AddComponent<Billboard>();
            }

            // PlayerController 직렬화 레퍼런스 주입
            var so = new SerializedObject(pc);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("_animator").objectReferenceValue = anim;
            so.ApplyModifiedProperties();

            // 7. Cinemachine 카메라 추적 연동
            var vcam = GameObject.FindFirstObjectByType<CinemachineCamera>();
            if (vcam == null)
            {
                var vcamObj = new GameObject("CinemachineCamera");
                vcam = vcamObj.AddComponent<CinemachineCamera>();
                Undo.RegisterCreatedObjectUndo(vcamObj, "Create CinemachineCamera");
            }
            vcam.Follow = playerObj.transform;
            vcam.LookAt = null;
            vcam.transform.localEulerAngles = new Vector3(30f, 0f, 0f);

            var follow = vcam.GetComponent<CinemachineFollow>();
            if (follow == null)
            {
                follow = vcam.gameObject.AddComponent<CinemachineFollow>();
            }
            follow.FollowOffset = new Vector3(0f, 8f, -12f);

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<CinemachineBrain>() == null)
            {
                Undo.AddComponent<CinemachineBrain>(cam.gameObject);
            }

            // 8. 씬 변경사항 저장
            EditorUtility.SetDirty(playerObj);
            EditorUtility.SetDirty(vcam.gameObject);
            if (cam != null) EditorUtility.SetDirty(cam.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[Setup] ✅ 종합 Animator Controller 빌드 및 루차도르 8방향 셋업이 완료되었습니다!");
            EditorUtility.DisplayDialog("완료", "루차도르 종합 애니메이터 8방향 셋업 완료!", "OK");
        }

        private static Sprite[] LoadSpritesFromSheet(string relativePath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(relativePath);
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                {
                    sprites.Add(s);
                }
            }
            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.ToArray();
        }
    }
}
