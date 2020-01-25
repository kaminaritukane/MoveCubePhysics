using BlankProject;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace Scripts.Sphere
{
    public class ObjectColorBehaviour : MonoBehaviour
    {
        [SerializeField] private Color color;

        private Renderer render;

        private void OnEnable()
        {
            render = GetComponent<Renderer>();
            SetColor(color);
        }

        public void SetColor(Color newColor)
        {
            color = newColor;
            render.material.color = color;
        }
    }
}