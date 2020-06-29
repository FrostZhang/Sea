using UnityEngine;

namespace vnc.FX
{
    public class WaterCamera : MonoBehaviour
    {
        public Material Wobble;
        public Color underwaterColor;
        public BlendMode Blend;
        public float seaHigh;
        [Header("Shaders"), Space]
        public Shader multiply;
        public Shader overlay;
        public Shader screen;

        //private void Update()
        //{
        //    //float yAxis = transform.position.y;
        //    //yAxis += Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
        //    //yAxis -= Input.GetKey(KeyCode.DownArrow) ? 1 : 0;
        //    //yAxis = Mathf.Clamp(yAxis, 10, 40);
        //    //transform.position = Vector3.up * yAxis;

        //    switch (Blend)
        //    {
        //        case BlendMode.Multiply:
        //            Wobble.shader = multiply;
        //            break;
        //        case BlendMode.Overlay:
        //            Wobble.shader = overlay;
        //            break;
        //        case BlendMode.Screen:
        //            Wobble.shader = screen;
        //            break;
        //        default:
        //            break;
        //    }
        //}

        //public void SetBlend(int mode)
        //{
        //    Blend = (BlendMode)mode;
        //}

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Wobble == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            float yAxis = transform.position.y;
            if (yAxis <= seaHigh)
            {
                Wobble.SetColor("_Color", underwaterColor);
                Graphics.Blit(source, destination, Wobble);
            }
            else
            {
                Wobble.SetColor("_Color", Color.white);
                Graphics.Blit(source, destination);
            }
        }
    }

    public enum BlendMode
    {
        Multiply, 
        Overlay,
        Screen
    }
}
