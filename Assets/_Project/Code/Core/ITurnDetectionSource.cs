using System;

namespace GlassRefrain.Core;

public interface ITurnDetectionSource {
    event Action<bool> SharpTurnDetected;
}
