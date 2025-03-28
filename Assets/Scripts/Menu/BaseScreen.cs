using UnityEngine;

namespace Menu
{
    public abstract class BaseScreen : MonoBehaviour
    {
        protected bool IsEnabled;

        public virtual void Show()
        {
            IsEnabled = true;
            gameObject.SetActive(true);
        }
        
        public virtual void Hide()
        {
            IsEnabled = false;
            gameObject.SetActive(false);
        }
        
        public virtual void Toggle()
        {
            if (IsEnabled)
                Hide();
            else
                Show();
        }
    }
}