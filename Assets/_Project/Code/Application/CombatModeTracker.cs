using System;
using R3;
using UnityEngine;

namespace GlassRefrain.Application;

public sealed class CombatModeTracker : IDisposable {
    private readonly ReactiveProperty<bool> _isInCombat;
    private IDisposable _countdownTimer;
    private float _lastActivityTime = -99f;
    private const float CombatExitDelay = 10f;

    public ReactiveProperty<bool> IsInCombat => _isInCombat;

    public CombatModeTracker() {
        _isInCombat = new ReactiveProperty<bool>(false);
    }

    public void NotifyCombatActivity() {
        _lastActivityTime = Time.time;
        if (!_isInCombat.Value) {
            _isInCombat.Value = true;
        }
        RestartCountdown();
    }

    public void ForceExitCombat() {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        if (_isInCombat.Value) {
            _isInCombat.Value = false;
        }
    }

    private void RestartCountdown() {
        _countdownTimer?.Dispose();
        _countdownTimer = Observable.Timer(TimeSpan.FromSeconds(CombatExitDelay))
            .Subscribe(_ => {
                if (Time.time - _lastActivityTime >= CombatExitDelay) {
                    _isInCombat.Value = false;
                }
            });
    }

    public void Dispose() {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        _isInCombat?.Dispose();
    }
}
