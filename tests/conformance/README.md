# Shared behavioral fixtures

`timelines.json` contains language-neutral samples extracted from the C# playback
tests and easing equations. Both the C# unit suite and the actual GDScript suite
consume this file. Deltas are incremental seconds; credit precedes the first
update. State numbers are delayed (0), playing (1), interval (2), completed (3).
The tolerance accommodates C#'s single-precision easing/progress values.

These fixtures establish timing/easing agreement, not full API or export parity.
Engine lifetime, callbacks and property writes have separate integration tests.

`fx.json` supplies shared samples of the FX shake signal, including negative noise
coordinates, signed seed extremes and the periodic lattice boundary. Noise is
quintic-interpolated value noise with a fixed 32-bit lattice hash. Both languages
check these samples, plus continuity, bounds, endpoints and sampling-order
independence in their FX suites. It requires no native noise resource.
