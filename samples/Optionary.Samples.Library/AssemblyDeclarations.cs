using Optionary;
using Optionary.Samples.Library;

// The assembly form, for a type that has no [Optionary] of its own, with an explicit section path. ":" nests sections, so the
// options live under "Samples" -> "Clock".
[assembly: Optionary<ClockSettings>("Samples:Clock")]
