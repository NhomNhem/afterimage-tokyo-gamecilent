# RaycastPro Warning Suppressions

This folder uses a scoped `csc.rsp` to suppress vendor warnings only.

## Suppressed warnings

### CS0618

Obsolete Unity API usage.

Examples:
- Physics2D.OverlapCircleNonAlloc
- Physics2D.RaycastNonAlloc
- Object.FindObjectOfType

### CS0628

New protected member declared in sealed type.

This is a vendor design/style issue.

### CS0414

Field is assigned but its value is never used.

Low-value vendor noise unless runtime behavior is affected.