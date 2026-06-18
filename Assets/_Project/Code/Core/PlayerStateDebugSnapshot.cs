using System.Collections.Generic;

namespace GlassRefrain.Core;

public readonly struct PlayerStateDebugSnapshot {
    public string Summary { get; }
    public IReadOnlyList<string> Details { get; }

    public PlayerStateDebugSnapshot(string summary, IReadOnlyList<string> details) {
        Summary = summary ?? string.Empty;
        Details = details ?? System.Array.Empty<string>();
    }
}
