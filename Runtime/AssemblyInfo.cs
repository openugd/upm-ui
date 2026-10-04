using System.Runtime.CompilerServices;

// The mesh algorithms behind the components (MeshMirror, TriangleCutter, GradientFrame, GradientTessellator)
// are internal: they are implementation details, not API. They make no engine calls, so the test assembly
// drives them directly and level1.sh in openugd/upm-tools can run those tests on .NET without Unity.
[assembly: InternalsVisibleTo("com.openugd.ui.tests")]
