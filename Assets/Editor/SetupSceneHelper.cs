using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using System.IO;

namespace ProjectAdventure.Editor
{
    public static class SetupSceneHelper
    {
        private const string TopDownFolder = "Assets/11.Sprites/TopDownCharacter/Character";

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

            // 2. 캐릭터 스프라이트 시트들 자동 슬라이스 (가로 32px 격자)
            string[] characterSheets = {
                "Character_Down.png",
                "Character_DownLeft.png",
                "Character_DownRight.png",
                "Character_Left.png",
                "Character_Right.png",
                "Character_Up.png",
                "Character_UpLeft.png",
                "Character_UpRight.png",
                "Character_SlashDownLeft.png",
                "Character_SlashDownRight.png",
                "Character_SlashUpLeft.png",
                "Character_SlashUpRight.png",
                "Character_RollDown.png",
                "Character_RollDownLeft.png",
                "Character_RollDownRight.png",
                "Character_RollLeft.png",
                "Character_RollRight.png",
                "Character_RollUp.png",
                "Character_RollUpLeft.png",
                "Character_RollUpRight.png"
            };

            foreach (string file in characterSheets)
            {
                string path = $"{TopDownFolder}/{file}";
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[Setup] 에셋 파일을 찾을 수 없습니다: {path}");
                    continue;
                }

                ConfigureAndSliceSprite(path, 32, 32);
            }
            AssetDatabase.Refresh();

            // 3. 스폰 높이 감지
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
            Undo.SetCurrentGroupName("Setup TopDownCharacter Player");

            // 4. Player 오브젝트 생성/탐색
            var playerObj = GameObject.Find("Player") ?? new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            playerObj.transform.position = spawnPos;

            var cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = playerObj.AddComponent<CharacterController>();
            }
            cc.center = new Vector3(0, 1f, 0); 
            cc.radius = 0.4f; 
            cc.height = 1.8f;

            var pc = playerObj.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = playerObj.AddComponent<PlayerController>();
            }

            // 5. Visual 자식 오브젝트 생성/탐색
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

            // Animator가 있으면 제거
            var oldAnim = visual.GetComponent<Animator>();
            if (oldAnim != null)
            {
                Object.DestroyImmediate(oldAnim);
            }

            // SpriteRenderer 세팅
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = visual.AddComponent<SpriteRenderer>();
            }
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            sr.receiveShadows = true;

            // 빌보드 컴포넌트
            if (visual.GetComponent<Billboard>() == null)
            {
                visual.AddComponent<Billboard>();
            }

            // 6. 슬라이스된 각 스프라이트 배열 로드
            Sprite[] sprWalkS  = LoadSpritesFromSheet($"{TopDownFolder}/Character_Down.png");
            Sprite[] sprWalkSE = LoadSpritesFromSheet($"{TopDownFolder}/Character_DownRight.png");
            Sprite[] sprWalkE  = LoadSpritesFromSheet($"{TopDownFolder}/Character_Right.png");
            Sprite[] sprWalkNE = LoadSpritesFromSheet($"{TopDownFolder}/Character_UpRight.png");
            Sprite[] sprWalkN  = LoadSpritesFromSheet($"{TopDownFolder}/Character_Up.png");
            Sprite[] sprWalkNW = LoadSpritesFromSheet($"{TopDownFolder}/Character_UpLeft.png");
            Sprite[] sprWalkW  = LoadSpritesFromSheet($"{TopDownFolder}/Character_Left.png");
            Sprite[] sprWalkSW = LoadSpritesFromSheet($"{TopDownFolder}/Character_DownLeft.png");

            Sprite[] sprRollS  = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollDown.png");
            Sprite[] sprRollSE = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollDownRight.png");
            Sprite[] sprRollE  = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollRight.png");
            Sprite[] sprRollNE = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollUpRight.png");
            Sprite[] sprRollN  = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollUp.png");
            Sprite[] sprRollNW = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollUpLeft.png");
            Sprite[] sprRollW  = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollLeft.png");
            Sprite[] sprRollSW = LoadSpritesFromSheet($"{TopDownFolder}/Character_RollDownLeft.png");

            Sprite[] sprSlashDL = LoadSpritesFromSheet($"{TopDownFolder}/Character_SlashDownLeft.png");
            Sprite[] sprSlashDR = LoadSpritesFromSheet($"{TopDownFolder}/Character_SlashDownRight.png");
            Sprite[] sprSlashUL = LoadSpritesFromSheet($"{TopDownFolder}/Character_SlashUpLeft.png");
            Sprite[] sprSlashUR = LoadSpritesFromSheet($"{TopDownFolder}/Character_SlashUpRight.png");

            // 기본 스프라이트 세팅
            if (sprWalkS != null && sprWalkS.Length > 0)
            {
                sr.sprite = sprWalkS[0];
            }

            // PlayerController 직렬화 매핑 바인딩
            var so = new SerializedObject(pc);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            
            SetSerializedSpriteArray(so.FindProperty("_animWalkS"), sprWalkS);
            SetSerializedSpriteArray(so.FindProperty("_animWalkSE"), sprWalkSE);
            SetSerializedSpriteArray(so.FindProperty("_animWalkE"), sprWalkE);
            SetSerializedSpriteArray(so.FindProperty("_animWalkNE"), sprWalkNE);
            SetSerializedSpriteArray(so.FindProperty("_animWalkN"), sprWalkN);
            SetSerializedSpriteArray(so.FindProperty("_animWalkNW"), sprWalkNW);
            SetSerializedSpriteArray(so.FindProperty("_animWalkW"), sprWalkW);
            SetSerializedSpriteArray(so.FindProperty("_animWalkSW"), sprWalkSW);

            SetSerializedSpriteArray(so.FindProperty("_animRollS"), sprRollS);
            SetSerializedSpriteArray(so.FindProperty("_animRollSE"), sprRollSE);
            SetSerializedSpriteArray(so.FindProperty("_animRollE"), sprRollE);
            SetSerializedSpriteArray(so.FindProperty("_animRollNE"), sprRollNE);
            SetSerializedSpriteArray(so.FindProperty("_animRollN"), sprRollN);
            SetSerializedSpriteArray(so.FindProperty("_animRollNW"), sprRollNW);
            SetSerializedSpriteArray(so.FindProperty("_animRollW"), sprRollW);
            SetSerializedSpriteArray(so.FindProperty("_animRollSW"), sprRollSW);

            SetSerializedSpriteArray(so.FindProperty("_animSlashDownLeft"), sprSlashDL);
            SetSerializedSpriteArray(so.FindProperty("_animSlashDownRight"), sprSlashDR);
            SetSerializedSpriteArray(so.FindProperty("_animSlashUpLeft"), sprSlashUL);
            SetSerializedSpriteArray(so.FindProperty("_animSlashUpRight"), sprSlashUR);
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

            Debug.Log("[Setup] ✅ TopDownCharacter 8방향 걷기, 점프구르기 및 4방향 베기 셋업이 완료되었습니다!");
            EditorUtility.DisplayDialog("완료", "TopDownCharacter 8방향 점프 셋업 완료!", "OK");
        }

        private static void ConfigureAndSliceSprite(string relativePath, int frameWidth, int frameHeight)
        {
            var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsToUnits = 32; // 32x32 크기이므로 32 PPU
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            string fileName = Path.GetFileNameWithoutExtension(relativePath);
            int width = fileName.Contains("Slash") ? 160 : 128; // Slash는 5프레임(160px), 그 외는 4프레임(128px)
            int frameCount = width / frameWidth;

            var metas = new System.Collections.Generic.List<SpriteMetaData>();
            for (int i = 0; i < frameCount; i++)
            {
                var meta = new SpriteMetaData();
                meta.rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                meta.name = $"{fileName}_{i}";
                meta.alignment = (int)SpriteAlignment.Center;
                metas.Add(meta);
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
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

        private static void SetSerializedSpriteArray(SerializedProperty prop, Sprite[] sprites)
        {
            prop.ClearArray();
            if (sprites == null) return;
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }
    }
}
