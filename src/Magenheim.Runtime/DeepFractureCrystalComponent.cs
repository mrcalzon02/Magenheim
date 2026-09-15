using System;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal enum DeepFractureCrystalFunction
    {
        EnvironmentalControl,
        DeathAnchor
    }

    internal sealed class DeepFractureCrystalComponent : MonoBehaviour, IDestructible
    {
        private const string HealthKeyPrefix = "magenheim_crystal_health_";
        private const string DestroyedKeyPrefix = "magenheim_crystal_destroyed_";
        private Character _owner;
        private ZNetView _view;
        private string _componentId;
        private DeepFractureCrystalFunction _function;
        private float _maximumHealth;
        private Renderer[] _renderers;
        private Collider[] _colliders;

        internal static DeepFractureCrystalComponent Attach(GameObject part, Character owner, ZNetView view, string componentId, DeepFractureCrystalFunction function, float maximumHealth)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (string.IsNullOrEmpty(componentId)) throw new ArgumentException("Crystal component identity is required.", nameof(componentId));
            if (maximumHealth <= 0f) throw new ArgumentOutOfRangeException(nameof(maximumHealth));

            var component = part.AddComponent<DeepFractureCrystalComponent>();
            component._owner = owner;
            component._view = view;
            component._componentId = componentId;
            component._function = function;
            component._maximumHealth = maximumHealth;
            component._renderers = part.GetComponentsInChildren<Renderer>(true);
            component._colliders = part.GetComponentsInChildren<Collider>(true);
            if (component._colliders.Length == 0)
            {
                var collider = part.AddComponent<BoxCollider>();
                collider.isTrigger = false;
                component._colliders = new Collider[] { collider };
            }
            component.RefreshState();
            return component;
        }

        public DestructibleType GetDestructibleType() { return DestructibleType.Default; }

        public void Damage(HitData hit)
        {
            if (hit == null || _owner == null || _view == null || !_view.IsValid()) return;
            if (!_view.IsOwner()) return;
            var zdo = _view.GetZDO();
            if (zdo == null || IsDestroyed(zdo)) return;

            var damage = hit.GetTotalDamage();
            if (damage <= 0f) return;
            var health = zdo.GetFloat(HealthKey(), _maximumHealth) - damage;
            zdo.Set(HealthKey(), Mathf.Max(0f, health));
            if (health <= 0f)
            {
                zdo.Set(DestroyedKey(), true);
                ApplyDestroyedState();
            }
        }

        private void OnEnable() { RefreshState(); }

        private void RefreshState()
        {
            if (_view == null || !_view.IsValid()) return;
            var zdo = _view.GetZDO();
            if (zdo != null && IsDestroyed(zdo)) ApplyDestroyedState();
        }

        private bool IsDestroyed(ZDO zdo) { return zdo.GetBool(DestroyedKey(), false); }
        private string HealthKey() { return HealthKeyPrefix + _componentId; }
        private string DestroyedKey() { return DestroyedKeyPrefix + _componentId; }

        private void ApplyDestroyedState()
        {
            if (_renderers != null) foreach (var renderer in _renderers) if (renderer != null) renderer.enabled = false;
            if (_colliders != null) foreach (var collider in _colliders) if (collider != null) collider.enabled = false;

            if (_function == DeepFractureCrystalFunction.EnvironmentalControl && _owner != null)
            {
                _owner.m_runSpeed = Mathf.Min(_owner.m_runSpeed, .75f);
                _owner.m_walkSpeed = Mathf.Min(_owner.m_walkSpeed, .35f);
            }
        }

        internal bool IsDeathAnchorIntact()
        {
            if (_function != DeepFractureCrystalFunction.DeathAnchor || _view == null || !_view.IsValid()) return false;
            var zdo = _view.GetZDO();
            return zdo != null && !IsDestroyed(zdo);
        }
    }
}
