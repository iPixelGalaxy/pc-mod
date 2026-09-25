using IPA.Loader;
using System;
using System.Reflection;
using UnityEngine;

namespace ScoreSaber.Features.Replays.Compatibility {
    internal sealed class Camera2ReplaySource : IDisposable {
        private readonly object _source;
        private readonly MethodInfo _setActive;
        private readonly MethodInfo _unregister;
        private readonly MethodInfo _updateWorld;
        private readonly MethodInfo _updateFromOrigin;
        private readonly object[] _worldArgs = new object[2];
        private readonly object[] _originArgs = new object[3];
        private bool _active;
        private bool _registered;

        public Camera2ReplaySource() {
            try {
                var assembly = PluginManager.GetPluginFromId("Camera2")?.Assembly;
                var sourcesType = assembly?.GetType("Camera2.SDK.ReplaySources");
                var sourceType = sourcesType?.GetNestedType("WorldSource");
                if (sourceType == null) return;

                var register = sourcesType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                _unregister = sourcesType.GetMethod("Unregister", BindingFlags.Public | BindingFlags.Static);
                _setActive = sourceType.GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance);
                _updateWorld = sourceType.GetMethod("UpdateWorld", new[] { typeof(Vector3), typeof(Quaternion) });
                _updateFromOrigin = sourceType.GetMethod("UpdateWorldFromOrigin", new[] { typeof(Transform), typeof(Vector3), typeof(Quaternion) });
                if (register == null || _unregister == null || _setActive == null || _updateWorld == null) return;

                _source = Activator.CreateInstance(sourceType, "ScoreSaberReplay");
                register.Invoke(null, new[] { _source });
                _registered = true;
            } catch (Exception ex) {
                Plugin.Log.Warn($"Camera2 replay source unavailable: {ex.Message}");
            }
        }

        public void UpdateFromOrigin(Transform origin, Vector3 localPosition, Quaternion localRotation) {
            if (!_registered) return;
            try {
                if (_updateFromOrigin != null) {
                    _originArgs[0] = origin;
                    _originArgs[1] = localPosition;
                    _originArgs[2] = localRotation;
                    _updateFromOrigin.Invoke(_source, _originArgs);
                } else {
                    UpdateWorld(origin.TransformPoint(localPosition), origin.rotation * localRotation);
                    return;
                }
                Activate();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Camera2 replay pose update failed: {ex.Message}");
                Dispose();
            }
        }

        public void UpdateWorld(Vector3 position, Quaternion rotation) {
            if (!_registered) return;
            try {
                _worldArgs[0] = position;
                _worldArgs[1] = rotation;
                _updateWorld.Invoke(_source, _worldArgs);
                Activate();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Camera2 replay pose update failed: {ex.Message}");
                Dispose();
            }
        }

        private void Activate() {
            if (_active) return;
            _setActive.Invoke(_source, new object[] { true });
            _active = true;
            Plugin.Log.Info("Camera2 ScoreSaber replay pose active");
        }

        public void Dispose() {
            if (!_registered) return;
            _registered = false;
            try {
                if (_active) _setActive.Invoke(_source, new object[] { false });
                _unregister.Invoke(null, new[] { _source });
            } catch (Exception ex) {
                Plugin.Log.Warn($"Camera2 replay source cleanup failed: {ex.Message}");
            }
            _active = false;
        }
    }
}
