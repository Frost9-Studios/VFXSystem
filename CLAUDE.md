# CLAUDE.md - VFXSystem Claude Code Instructions

## Project Layout
This is a Unity 6.3 project.

---

## MCP Tool Routing

You have three MCP servers. Use them intentionally:

### 1) unityMCP - Unity Editor / Project Mutation
Use for:
- editing scripts/assets/scenes/prefabs
- reading Unity Console
- running tests
- finding GameObjects / project objects

Only mutate the project when the user asks, or when changes are clearly required to complete the request.
When in doubt, read state first (search/read tools) before applying edits.

### 2) unity-docs - Unity Documentation (Engine API)
Use for Unity engine API documentation and version-specific behavior.

**Unity 6.3 version rules (project = 6000.3):**
- `search_unity_docs`: always call with `version: "6000.2"` (6000.3 search is unreliable).
- `get_unity_api_doc`: prefer `version: "6000.3"` (matches the project). If it fails, retry with `version: "6000.2"`.
- If search returns no results, use:
  1) `suggest_unity_classes(partial_name: "...")`
  2) `get_unity_api_doc(class_name: "ExactName", version: "6000.3")` (fallback to 6000.2 if needed)

### 3) MCP_DOCKER - Context7 / Third-Party Library Docs
Use for third-party libraries (e.g., VContainer, R3, UniTask, FMOD, DOTween, etc.).
Typical flow:
- `resolve-library-id`
- `get-library-docs`

Prefer this over guessing library behavior from memory.

---

## Tool Output Rules (avoid silent tool calls)
When using **unity-docs** or **MCP_DOCKER**, always print at least:
- result count (or explicitly "no results"),
- top 3 relevant items (titles/symbols),
- the exact symbol name used for `get_unity_api_doc` (and method/property name if applicable).

Never return an empty response. If a tool returns nothing useful, say so and try a simpler query.

---

## Assembly Definitions (Code Organization)
Place scripts into the correct Assembly Definition based on function. Do not create circular dependencies.

| Assembly             | Namespace           | Purpose                                                                 |
|----------------------|---------------------|-------------------------------------------------------------------------|
| Frost9.VFX           | Frost9.VFX          | Runtime visual-effects system. Service-first VFX playback, pooling,     |
|                      |                     | catalogs, runners, and handles. Unity-runtime only, no game dependencies. |
| Frost9.VFX.Editor    | Frost9.VFX.Editor   | Editor-only tooling for Frost9.VFX. Catalog validation, authoring       |
|                      |                     | helpers, diagnostics, and optional code generation. Not included in builds. |
| Frost9.VFX.Tests     | Frost9.VFX.Tests    | Automated tests for Frost9.VFX. Validates pooling behavior, lifecycle,  |
|                      |                     | handle safety, reset contracts, and service APIs. Editor/Test only.     |

---

## Development Workflow

### 1) Planning (required for complex features)
Before generating code for complex features, provide a brief plan:

- **Goal:** What is being built?
- **Assembly:** Which `.asmdef` will this belong to?
- **Dependencies:** Do I need any dependencies e.g. VConatiner?
- **Design:** Brief note on DI usage and/or EventBus messaging (what publishes, what subscribes, and where).

### 2) Documentation / Comments
- Add XML `<summary>` tags to all public classes and members.
- Code should be self-documenting; use comments only for complex logic (not to restate what the code already says).

---

## Build & Test
- Open project with Unity Hub editor `6000.3.9f1` (see `ProjectSettings/ProjectVersion.txt`).
- Build: Unity Editor -> `File -> Build Settings -> Build`.
- Tests: Unity Test Runner (`Window -> General -> Test Runner`).
- Add tests under:
  - `Assets\com.frost9.vfx\Tests`
  Use `*Tests.cs` naming.

---
`.
