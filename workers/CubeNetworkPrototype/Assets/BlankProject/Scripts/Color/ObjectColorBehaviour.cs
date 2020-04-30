using BlankProject;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace Scripts.Sphere
{
    public class ObjectColorBehaviour : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private Material red;
        [SerializeField] private Material blue;

        private Renderer render;

        private void OnEnable()
        {
            render = GetComponent<Renderer>();
            SetColor(color);
        }

        public void SetColor(Color newColor)
        {
            //render.sharedMaterial.color = newColor;
            if (newColor == Color.blue)
            {
                render.sharedMaterial = blue;
            }
            else if ( newColor == Color.red )
            {
                render.sharedMaterial = red;
            }
        }
    }
}