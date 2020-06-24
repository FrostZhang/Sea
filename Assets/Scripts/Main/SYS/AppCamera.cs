using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using com.ootii.Cameras;
using DG.Tweening;
using System;

// Copyright (C) 2020 All Rights Reserved.
// Detail:AppCamera  Red  2020/1/10
// Modify:    Version:1.0.0

public class AppCamera : IAppUpdate, ILateUpdate
{
    Camera mainCa;
    RaycastHit[] hits = new RaycastHit[5];
    int hitsNum;
    public Transform transform => mainCa.transform;
    //CameraController followCamera;
    public CameraController m_cameraController;
    public Transform m_targetTrans;

    private float m_desiredDistance = 1;
    private float m_zoomRate = 30;
    private float m_minDistance = 1;
    private float m_maxDistance = 3000;
    private float m_zoomDampening = 3;

    public AppCamera(Transform tr)
    {
        mainCa = tr.Find("Ca/Camera Rig/Main Camera").GetComponent<Camera>();
        mainCa.depthTextureMode = DepthTextureMode.DepthNormals;
        m_cameraController = tr.Find("Ca/Camera Rig").GetComponent<CameraController>();
        m_targetTrans = tr.Find("Ca/CameraTarget");

        GameApp.Ins.StartCoroutine(Ini());
        //followCamera = mainCa.GetComponent<CameraController>();
        GameApp.Ins.Updates.Add(this);
        GameApp.Ins.LateUpdates.Add(this);
    }

    public void Follow(Transform tr, float dis)
    {
        SwitchToThirdPersonView(tr, dis, false, false, 35);
    }

    IEnumerator Ini()
    {
        yield return null;
        //var giz = EditorGizmoSystem.Instance;
        //var sle = EditorObjectSelection.Instance;
        //RuntimeEditorApplication.Instance.gameObject.SetActive(false);
    }

    public static implicit operator Camera(AppCamera appCa)
    {
        return appCa.mainCa;
    }

    public bool ScreenPointToWorld(ref Vector3 pos, float maxDistance = 50, int layer = -1)
    {
        if (layer != -1)
            hitsNum = Physics.RaycastNonAlloc(mainCa.ScreenPointToRay(pos), hits, maxDistance, layer);
        else
            hitsNum = Physics.RaycastNonAlloc(mainCa.ScreenPointToRay(pos), hits, maxDistance);
        if (hitsNum > 0)
        {
            pos = hits[0].point;
            return true;
        }
        return false;
    }

    public Transform ScreenPointGetTarget(ref Vector3 pos, float maxDistance = 50, int layer = -1)
    {
        if (ScreenPointToWorld(ref pos, maxDistance, layer))
        {
            return hits[hitsNum - 1].transform;
        }
        return default(Transform);
    }

    public bool WorldToScreenPoint(ref Vector3 pos)
    {
        pos = mainCa.WorldToScreenPoint(pos);
        if (pos.x <= 0 || pos.x > Screen.width || pos.y < 0 || pos.y > Screen.height)
        {
            return false;
        }
        return true;
    }

    public void WorldToScreenPointNonCheck(ref Vector3 pos)
    {
        pos = mainCa.WorldToScreenPoint(pos);
    }

    public void Update()
    {
        if (GameApp.Ins)
        {
            // 按住滚轮中间平移
            //if (Input.GetMouseButton(2))
            //{
            //    CameraMotor activeMotor = m_cameraController.ActiveMotor;
            //    float deltaX = Input.GetAxis("Mouse X");
            //    Vector3 deltaVec = m_cameraController.transform.right * Time.deltaTime * deltaX * activeMotor.Distance;
            //    m_targetTrans.position -= deltaVec;

            //    float deltaY = Input.GetAxis("Mouse Y");
            //    deltaVec = m_cameraController.transform.up * Time.deltaTime * deltaY * activeMotor.Distance;
            //    m_targetTrans.position -= deltaVec;
            //}
        }
    }

    public void LateUpdate()
    {
        CameraMotor activeMotor = m_cameraController.ActiveMotor;
        if (activeMotor.Name == "Targeting" && !m_cameraController.InputSource.IsPressed(KeyCode.Mouse2))
        {
            // affect the desired Zoom distance if we roll the scrollwheel
            m_desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * m_zoomRate * Mathf.Abs(m_desiredDistance);
            // clamp the zoom min/max
            m_desiredDistance = Mathf.Clamp(m_desiredDistance, m_minDistance, m_maxDistance);
            // For smoothing of the zoom, lerp distance
            activeMotor.Distance = Mathf.Lerp(activeMotor.Distance, m_desiredDistance, Time.deltaTime * m_zoomDampening);
        }
    }

    protected Tweener m_tweenerToFPV, m_tweenerFPV;
    public void SwitchToFirstPersonView()
    {
        CameraMotor activeMotor = m_cameraController.ActiveMotor;
        if (activeMotor.Name == "1st Person View")
        {
            return;
        }

        m_enableChangeCameraMode = false;
        //m_FPSControllerTrans.gameObject.SetActive(true);

        //if (m_targetTrans.parent != m_FPSControllerTrans.parent)
        //{
        //    m_targetTrans.SetParent(m_FPSControllerTrans.parent, true);
        //}

        Vector3 targetPos = m_targetTrans.localPosition;
        m_targetTrans.position = m_cameraController.transform.localPosition;
        m_targetTrans.rotation = m_cameraController.transform.localRotation;
        m_tweenerToFPV = m_targetTrans.DOLocalMove(m_cameraController.transform.localPosition, m_tweenTime);
        Vector3 ro = m_targetTrans.eulerAngles;
        ro.x = ro.z = 0;
        Quaternion targetRot = m_targetTrans.localRotation;
        //m_targetTrans.DOLocalRotateQuaternion(targetRot, m_tweenTime);
        m_targetTrans.DOLocalRotate(ro /*targetRot.eulerAngles*/, m_tweenTime, RotateMode.Fast);

        //TransitionMotor transMotor = m_cameraController.GetMotor<TransitionMotor>("Targeting Out");
        //m_cameraController.ActivateMotor(transMotor);

        //m_cameraController.AnchorOffset = new Vector3(0.0f, 1.8f, 0.0f);
        DOTween.To(() => m_cameraController.AnchorOffset, x => m_cameraController.AnchorOffset = x, new Vector3(0.0f, 0, 0.0f), m_tweenTime);

        CameraMotor motor = m_cameraController.GetMotor<CameraMotor>("1st Person View");

        motor.IsEnabled = true;
        m_cameraController.ActivateMotor(motor);

        // 动画开始时禁用相机控制
        motor.IsEnabled = false;

        // 启用通过FOV实现的缩放
        m_cameraController.IsZoomEnabled = true;

        m_tweenerToFPV.OnComplete(OnSwitchedToFirstPersonView);
    }

    protected void OnSwitchedToFirstPersonView()
    {
        // 动画结束时启用相机控制
        CameraMotor activeMotor = m_cameraController.ActiveMotor;
        activeMotor.IsEnabled = true;
        m_enableChangeCameraMode = true;
    }

    public float m_defaultOffsetAngle = 45;
    public void SwitchToThirdPersonView(Transform targetTrans, float distance = -1.0f, bool changeParent = false, bool overrideOffsetAngle = false, float offsetAngle = 0.0f)
    {
        Vector3 targetPos = targetTrans.GetMaxBounds().center;
        Quaternion targetRot = targetTrans.rotation;

        if (changeParent)
        {
            m_targetTrans.SetParent(targetTrans, true);
            //m_targetTrans.localPosition = Vector3.zero;
            //m_targetTrans.localRotation = Quaternion.identity;
            //m_targetTrans.localScale = Vector3.zero;

            targetPos = Vector3.zero;
            targetRot = Quaternion.identity;
        }
        //else
        //{
        //    if (m_targetTrans.parent != transform)
        //    {
        //        m_targetTrans.SetParent(transform, true);
        //    }
        //}

        float angle = overrideOffsetAngle ? offsetAngle : m_defaultOffsetAngle;
        Quaternion camDefaultRot = targetTrans.rotation * Quaternion.Euler(Vector3.right * angle);

        SwitchToThirdPersonView(targetPos, targetRot, camDefaultRot, distance);
    }

    public void SwitchToThirdPersonView(Transform targetTrans, Vector3 angle, float distance = -1.0f, bool changeParent = false, bool overrideOffsetAngle = false)
    {
        Vector3 targetPos = targetTrans.GetMaxBounds().center;
        Quaternion targetRot = targetTrans.rotation;

        if (changeParent)
        {
            m_targetTrans.SetParent(targetTrans, true);
            //m_targetTrans.localPosition = Vector3.zero;
            //m_targetTrans.localRotation = Quaternion.identity;
            //m_targetTrans.localScale = Vector3.zero;

            targetPos = Vector3.zero;
            targetRot = Quaternion.identity;
        }
        //else
        //{
        //    if (m_targetTrans.parent != transform)
        //    {
        //        m_targetTrans.SetParent(transform, true);
        //    }
        //}

        Quaternion camDefaultRot = targetTrans.rotation * Quaternion.Euler(angle);

        SwitchToThirdPersonView(targetPos, targetRot, camDefaultRot, distance);
    }

    private bool m_enableChangeCameraMode;
    protected Tweener m_tweenerPosToTPV, m_tweenerRotToTPV;
    public float m_tweenTime = 2.0f;
    public void SwitchToThirdPersonView(Vector3 targetPos, Quaternion targetRot, Quaternion camDefaultRot, float distance = -1.0f)
    {
        if (m_tweenerPosToTPV != null)
        {
            if (m_tweenerPosToTPV.IsPlaying())
            {
                //return;
                m_tweenerPosToTPV.Kill();
            }

            m_tweenerPosToTPV = null;
        }
        if (m_tweenerRotToTPV != null)
        {
            if (m_tweenerRotToTPV.IsPlaying())
            {
                //return;
                m_tweenerRotToTPV.Kill();
            }

            m_tweenerRotToTPV = null;
        }

        m_enableChangeCameraMode = false;

        // 位姿动画变化
        m_tweenerPosToTPV = m_targetTrans.DOLocalMove(targetPos, m_tweenTime);
        //m_targetTrans.DOLocalRotateQuaternion(targetRot, m_tweenTime);
        m_tweenerRotToTPV = m_targetTrans.DOLocalRotate(targetRot.eulerAngles, m_tweenTime, RotateMode.Fast);

        m_tweenerPosToTPV.OnComplete(OnSwitchedToThirdPersonView);

        // 设置相机的最终默认朝向
        //m_cameraController._Transform.DOLocalRotateQuaternion(camDefaultRot, m_tweenTime);
        m_cameraController._Transform.DOLocalRotate(camDefaultRot.eulerAngles, m_tweenTime, RotateMode.Fast);

        CameraMotor activeMotor = m_cameraController.ActiveMotor;
        if (activeMotor.Name == "Targeting")
        {
            // 距离动态变化
            if (distance > 0)
            {
                m_desiredDistance = distance;
                DOTween.To(() => activeMotor.Distance, x => activeMotor.Distance = x, distance, m_tweenTime);
            }

            return;
        }

        //m_FPSControllerTrans.gameObject.SetActive(false);

        //if (activeMotor.Name == "1st Person View")
        //{
        //    m_targetTrans.position = m_FPSControllerTrans.position;
        //    m_targetTrans.rotation = m_FPSControllerTrans.rotation;
        //}

        //TransitionMotor transMotor = m_cameraController.GetMotor<TransitionMotor>("Targeting In");
        //m_cameraController.ActivateMotor(transMotor);

        //m_cameraController.AnchorOffset = new Vector3(0.0f, 0.0f, 0.0f);
        DOTween.To(() => m_cameraController.AnchorOffset, x => m_cameraController.AnchorOffset = x, new Vector3(0.0f, 0.0f, 0.0f), m_tweenTime);

        CameraMotor motor = m_cameraController.GetMotor<CameraMotor>("Targeting");
        m_cameraController.ActivateMotor(motor);

        m_desiredDistance = motor.Distance;

        // 距离动态变化
        if (distance > 0)
        {
            m_desiredDistance = distance;
            DOTween.To(() => motor.Distance, x => motor.Distance = x, distance, m_tweenTime);
        }

        // 禁用通过FOV实现的缩放
        DOTween.To(() => m_cameraController.Camera.fieldOfView, x => m_cameraController.Camera.fieldOfView = x, m_cameraController.OriginalFOV, m_tweenTime);
        m_cameraController.IsZoomEnabled = false;
    }

    private void OnSwitchedToThirdPersonView()
    {
        CameraMotor activeMotor = m_cameraController.ActiveMotor;
        activeMotor.IsEnabled = true;
        m_enableChangeCameraMode = true;
    }
}
