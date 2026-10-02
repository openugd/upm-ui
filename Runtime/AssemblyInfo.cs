using System.Runtime.CompilerServices;

// The mesh algorithms behind the components (MeshMirror, TriangleCutter, GradientFrame, GradientTessellator)
// are internal: they are implementation details, not API. They make no engine calls, so the test assembly
// drives them directly and level 1 can run those tests without Unity.
[assembly: InternalsVisibleTo("com.openugd.ui.tests")]
