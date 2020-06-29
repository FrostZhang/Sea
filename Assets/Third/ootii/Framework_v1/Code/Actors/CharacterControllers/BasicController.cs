using UnityEngine;

namespace com.ootii.Actors
{
    public class BasicController : MonoBehaviour
    {
        public Transform Camera = null;

        public bool UseGamepad = false;

        public bool MovementRelative = true;

        public float MovementSpeed = 3f;

        public bool RotationEnabled = true;

        public bool RotateToInput = false;

        public float RotationSpeed = 180f;

        protected Transform mTransform = null;

        public void Awake()
        {
            mTransform = gameObject.transform;
        }

        public void Update()
        {
            if (RotationEnabled)
            {
                float lYaw = (UnityEngine.Input.GetKey(KeyCode.E) ? 1f : 0f);
                lYaw = lYaw - (UnityEngine.Input.GetKey(KeyCode.Q) ? 1f : 0f);

                if (lYaw != 0f)
                {
                    mTransform.rotation = mTransform.rotation * Quaternion.AngleAxis(lYaw * RotationSpeed * Time.deltaTime, Vector3.up);
                }
            }

            Vector3 lMovement = Vector3.zero;

            lMovement.z = (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);
            lMovement.z = lMovement.z - (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);

            lMovement.x = (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);
            lMovement.x = lMovement.x - (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);

            if (UseGamepad && lMovement.x == 0f && lMovement.z == 0f)
            {
                lMovement.z = UnityEngine.Input.GetAxis("Vertical");
                lMovement.x = UnityEngine.Input.GetAxis("Horizontal");
            }

            if (RotateToInput && Camera != null && lMovement.sqrMagnitude > 0f)
            {
                Quaternion lCameraRotation = Quaternion.Euler(0f, Camera.rotation.eulerAngles.y, 0f);
                mTransform.rotation = Quaternion.LookRotation(lCameraRotation * lMovement, Vector3.up);
                lMovement.z = lMovement.magnitude;
                lMovement.x = 0f;
            }

            if (lMovement.magnitude >= 1f) { lMovement.Normalize(); }
            if (MovementRelative) { lMovement = mTransform.rotation * lMovement; }

            mTransform.position = mTransform.position + (lMovement * (MovementSpeed * Time.deltaTime));
        }
    }
}
