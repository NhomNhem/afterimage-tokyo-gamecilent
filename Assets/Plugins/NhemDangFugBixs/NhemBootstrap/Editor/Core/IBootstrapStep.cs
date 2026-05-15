using System.Threading;
using UnityEngine;

namespace NhemBootStrap.Editor.Core {

    /// <summary>Defines a single synchronous bootstrap operation.</summary>
    public interface IBootstrapStep {
        /// <summary>Display name shown in the Bootstrap Window step list.</summary>
        string Name { get; }
        /// <summary>Returns <c>true</c> when the step's side effects are already present in the project and the step can be skipped.</summary>
        bool CheckCompleted();
        /// <summary>Executes the step's logic using the provided <paramref name="context"/>.</summary>
        /// <param name="context">Shared execution context for logging, config access, and cancellation.</param>
        void Execute(BootstrapContext context);
    }

    /// <summary>Opt-in async execution. Runner checks for this interface first.</summary>
    public interface IAsyncBootstrapStep : IBootstrapStep {
        /// <summary>Executes the step asynchronously. Runner awaits this when available.</summary>
        Awaitable ExecuteAsync(BootstrapContext context, CancellationToken ct);
    }
}
