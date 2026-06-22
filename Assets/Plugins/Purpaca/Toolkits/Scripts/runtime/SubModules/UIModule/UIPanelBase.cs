using UnityEngine;

namespace Purpaca.UI
{
    /// <summary>
    /// UI面板基类
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public abstract class UIPanelBase : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasGroup canvasGroup;

        private UIPanelBase _root = null, _top = null;
        private UIPanelBase _previous = null, _next = null;

        private bool _isPanelActive = false;
        private bool _isPanelEverActivate = false;

        /// <summary>
        /// 当前UI面板是否处于活跃状态？
        /// </summary>
        /// <remarks>活跃状态，即当前UI面板在其所在的UI面板栈中处于顶部位置且为可交互状态</remarks>
        public bool IsPanelActive => _isPanelActive;

        /// <summary>
        /// 此UI面板所在UI面板栈的最底层面板（谨慎）
        /// </summary>
        public UIPanelBase RootPanel
        {
            get
            {
                _root = _root == null ? this : _root;
                while (_root._previous != null)
                {
                    _root = _root.PreviousPanel;
                    _root._root = _root;
                }

                return _root;
            }
        }

        /// <summary>
        /// 此UI面板所在UI面板栈的最上层面板
        /// </summary>
        public UIPanelBase TopPanel
        {
            get
            {
                _top = _top == null ? this : _top;
                while (_top.NextPanel != null)
                {
                    _top = _top.NextPanel;
                    _top._top = _top;
                }

                return _top;
            }
        }

        /// <summary>
        /// 此UI面板上层的UI面板
        /// </summary>
        public UIPanelBase NextPanel { get => _next; private set => _next = value; }

        /// <summary>
        /// 此UI面板下层的UI面板
        /// </summary>
        public UIPanelBase PreviousPanel { get => _previous; private set => _previous = value; }

        protected Canvas Canvas
        {
            get
            {
                canvas = canvas == null? GetComponent<Canvas>() : canvas;
                return canvas;
            }
        }

        protected CanvasGroup CanvasGroup
        {
            get
            {
                canvasGroup = canvas == null ? GetComponent<CanvasGroup>() : canvasGroup;
                return canvasGroup;
            }
        }

        /// <summary>
        /// 首次激活此UI面板
        /// </summary>
        /// <remarks>通过此方法激活一个尚未激活的UI面板，会创建一个以当前UI面板为根的新UI面板栈</remarks>
        public void Activate() 
        {
            if (_isPanelEverActivate) 
            {
                Debug.LogWarning($"UI面板 \"{GetType().FullName}\" on gameobject \"{gameObject.name}\" 已经激活过！");
                return;
            }

            if (!enabled) enabled = true;
            if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
            CanvasGroup.alpha = 1;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            OnActivate();
            _isPanelActive = true;
            _isPanelEverActivate = true;
        }

        /// <summary>
        /// 首次激活此UI面板到目标UI面板栈的顶层
        /// </summary>
        /// <param name="hidePrevious">是否隐藏之前的顶层UI面板？（无论是否隐藏，都将失活之前的顶层UI面板）</param>
        /// <remarks>通过此方法激活一个尚未激活的UI面板，如果目标UI面板栈为null，会创建一个以当前UI面板为根的新UI面板栈</remarks>
        public void Activate(UIPanelBase targetStack, bool hidePrevious = true)
        {
            if (targetStack == null || ReferenceEquals(this, targetStack))
            {
                Activate();
            }
            else
            {
                targetStack.PushUIPanelToTopAndActivate(this, hidePrevious);
            }
        }

        /// <summary>
        /// 将一个UI面板首次激活到当前UI面板栈的顶层
        /// </summary>
        /// <param name="hidePrevious">是否隐藏之前的顶层UI面板？（无论是否隐藏，都将失活之前的顶层UI面板）</param>
        public void PushUIPanelToTopAndActivate(UIPanelBase panel, bool hidePrevious = true)
        {
            if (panel._isPanelEverActivate)
            {
                Debug.LogWarning($"UI面板 \"{GetType().FullName}\" on gameobject \"{gameObject.name}\" 已经激活过！");
                return;
            }

            var top = TopPanel;
            top.NextPanel = panel;
            top.CanvasGroup.alpha = hidePrevious ? 0 : top.CanvasGroup.alpha;
            top.CanvasGroup.interactable = false;
            top.CanvasGroup.blocksRaycasts = !hidePrevious;
            top.OnDeactive();
            top._isPanelActive = false;

            panel.PreviousPanel = top;
            if (!panel.enabled) panel.enabled = true;
            if (!panel.gameObject.activeInHierarchy) panel.gameObject.SetActive(true);
            panel.Canvas.sortingOrder = top.Canvas.sortingOrder + 1;
            panel.CanvasGroup.alpha = 1;
            panel.CanvasGroup.interactable = true;
            panel.canvasGroup.blocksRaycasts = true;
            panel.OnActivate();
            panel._isPanelActive = true;
            panel._isPanelEverActivate = true;
        }

        /// <summary>
        /// 关闭此UI面板（会连带依次关闭所有在此UI面板上层的UI面板）
        /// </summary>
        public void Close()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// 当前UI面板栈是否包含指定的UI面板？
        /// </summary>
        public bool IsStackContains(UIPanelBase panel) 
        {
            var result = false;

            var current = RootPanel;
            while (current != null)
            {
                result = ReferenceEquals(current, panel);
                current = current.NextPanel;
            }

            return result;
        }

        /// <summary>
        /// 将当前UI面板所在的UI面板栈续接在指定的UI面板所在的UI面板栈后
        /// </summary>
        public void AttachTo(UIPanelBase entryOfOtherStack)
        {
            if (entryOfOtherStack.IsStackContains(this))
            {
                Debug.LogError("不能把当前UI面板所在的UI面板栈续接在指定的UI面板所在的UI面板栈后，它们处于同一个UI面板栈中！");
                return;
            }

            var top = entryOfOtherStack.TopPanel;
            var root = RootPanel;

            top.NextPanel = root;
            top.CanvasGroup.alpha = 0;
            top.CanvasGroup.interactable = false;
            top.CanvasGroup.blocksRaycasts = false;
            top.OnDeactive();
            top._isPanelActive = false;

            root.PreviousPanel = top;
        }

        #region 子类虚方法
        /// <summary>
        /// 当此UI面板初始化时调用
        /// </summary>
        /// <remarks>将在组件的Awake()生命周期方法中执行</remarks>
        protected virtual void OnInit() { }

        /// <summary>
        /// 当此UI面板被激活时调用
        /// </summary>
        protected virtual void OnActivate() { }

        /// <summary>
        /// 当此UI面板被失活时调用（当此UI面板被销毁时不会调用此方法）
        /// </summary>
        protected virtual void OnDeactive() { }

        /// <summary>
        /// 当此UI面板被关闭时或销毁时调用
        /// </summary>
        protected virtual void OnClose() { }
        #endregion

        /// <summary>
        /// 激活此UI面板下层的（先前的）UI面板
        /// </summary>
        private void ActivatePreviousPanel()
        {
            if (PreviousPanel != null)
            {
                PreviousPanel.CanvasGroup.alpha = 1;
                PreviousPanel.CanvasGroup.interactable = true;
                PreviousPanel.CanvasGroup.blocksRaycasts = true;
                _previous.OnActivate();
                _previous._isPanelActive = true;
            }
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();

            OnInit();

            if (!_isPanelEverActivate)
            {
                CanvasGroup.alpha = 0;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDestroy()
        {
            if (NextPanel != null)
            {
                NextPanel.Close();
            }

            ActivatePreviousPanel();
            OnClose();
        }
    }
}