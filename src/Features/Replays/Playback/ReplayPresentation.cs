using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScoreSaber.Features.Replays.Playback {
    internal sealed class ReplayPresentation : IDisposable {
        private readonly PlayerVRControllersManager _controllers;
        private readonly List<AudioListener> _disabledListeners = new List<AudioListener>();
        private DeactivateVRControllersOnFocusCapture _focusCapture;
        private bool _focusCaptureWasEnabled;
        private bool _leftWasActive;
        private bool _rightWasActive;
        private bool _leftWasEnabled;
        private bool _rightWasEnabled;
        private bool _leftWasAutoplay;
        private bool _rightWasAutoplay;
        private bool _sabersPrepared;
        private AudioListener _replayListener;
        private bool _replayListenerWasEnabled;
        private bool _createdReplayListener;

        public ReplayPresentation(PlayerVRControllersManager controllers) {
            _controllers = controllers;
        }

        public void PrepareSabers() {
            var left = _controllers.leftHandVRController;
            var right = _controllers.rightHandVRController;
            _leftWasActive = left.gameObject.activeSelf;
            _rightWasActive = right.gameObject.activeSelf;
            _leftWasEnabled = left.enabled;
            _rightWasEnabled = right.enabled;
            _leftWasAutoplay = left.autoPlayActive;
            _rightWasAutoplay = right.autoPlayActive;

            _focusCapture = _controllers.GetComponent<DeactivateVRControllersOnFocusCapture>();
            if (_focusCapture != null) {
                _focusCaptureWasEnabled = _focusCapture.enabled;
                _focusCapture.enabled = false;
            }

            _controllers.SetupAutoplayForAllControllers();
            left.gameObject.SetActive(true);
            right.gameObject.SetActive(true);
            _sabersPrepared = true;
            Plugin.Log.Info($"Replay saber controllers: left={left.gameObject.activeInHierarchy}, right={right.gameObject.activeInHierarchy}");
        }

        public void AttachAudioListener(Camera replayCamera) {
            foreach (var listener in Resources.FindObjectsOfTypeAll<AudioListener>()) {
                if (listener != null && listener.isActiveAndEnabled && listener.gameObject != replayCamera.gameObject) {
                    _disabledListeners.Add(listener);
                    listener.enabled = false;
                }
            }

            _replayListener = replayCamera.GetComponent<AudioListener>();
            if (_replayListener == null) {
                _replayListener = replayCamera.gameObject.AddComponent<AudioListener>();
                _createdReplayListener = true;
            } else {
                _replayListenerWasEnabled = _replayListener.enabled;
            }
            _replayListener.enabled = true;
            Plugin.Log.Info($"Replay audio listener: {_replayListener.name}");
        }

        public void Dispose() {
            if (_replayListener != null) {
                if (_createdReplayListener) UnityEngine.Object.Destroy(_replayListener);
                else _replayListener.enabled = _replayListenerWasEnabled;
            }
            foreach (var listener in _disabledListeners) {
                if (listener != null) listener.enabled = true;
            }
            _disabledListeners.Clear();

            if (!_sabersPrepared || _controllers == null) return;
            var left = _controllers.leftHandVRController;
            var right = _controllers.rightHandVRController;
            if (left != null) {
                left.autoPlayActive = _leftWasAutoplay;
                left.enabled = _leftWasEnabled;
                left.gameObject.SetActive(_leftWasActive);
            }
            if (right != null) {
                right.autoPlayActive = _rightWasAutoplay;
                right.enabled = _rightWasEnabled;
                right.gameObject.SetActive(_rightWasActive);
            }
            if (_focusCapture != null) _focusCapture.enabled = _focusCaptureWasEnabled;
        }
    }
}
