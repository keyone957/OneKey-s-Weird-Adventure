using UnityEngine;

namespace ProjectAdventure
{
    [DisallowMultipleComponent]
    public class Billboard : MonoBehaviour
    {
        private Transform _cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("[Billboard] Main Camera not found in the scene. Make sure your main camera has the 'MainCamera' tag.");
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                if (Camera.main != null)
                {
                    _cameraTransform = Camera.main.transform;
                }
                return;
            }

            // 카메라의 회전을 복사하여 스프라이트가 카메라 평면과 완벽하게 평행을 유지하도록 합니다.
            transform.rotation = _cameraTransform.rotation;
        }
    }
}
