using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;

namespace pru213
{
    /// <summary>
    /// Action này sẽ tìm GameObject ServerCore trên Scene và gán nó làm Target cho AIBrain của quái vật.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AIActionSetServerCoreAsTarget")]
    public class AIActionSetServerCoreAsTarget : AIAction
    {
        protected Transform _serverCoreTransform;

        /// <summary>
        /// Khởi tạo và tìm ServerCore
        /// </summary>
        public override void Initialization()
        {
            base.Initialization();
            
            // Tìm ServerCore trên Scene (vì nó là duy nhất)
            ServerCore core = Object.FindFirstObjectByType<ServerCore>();
            if (core != null)
            {
                _serverCoreTransform = core.transform;
            }
            else
            {
                Debug.LogWarning("AIActionSetServerCoreAsTarget: Không tìm thấy ServerCore trên Scene!");
            }
        }

        /// <summary>
        /// Action được thực thi ở mỗi frame trong State tương ứng
        /// </summary>
        public override void PerformAction()
        {
            SetTarget();
        }

        /// <summary>
        /// Gán ServerCore làm Target cho Brain
        /// </summary>
        protected virtual void SetTarget()
        {
            if (_serverCoreTransform != null && _brain.Target != _serverCoreTransform)
            {
                _brain.Target = _serverCoreTransform;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            SetTarget();
        }
    }
}
