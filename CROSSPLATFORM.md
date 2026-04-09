# Zircon Cross-Platform Port Guide

## What is Zircon?

Zircon is a complete MMORPG implementation of **Legend of Mir 3** — a Korean-style 2D isometric MMORPG. It contains a game client, game server, and supporting tools (patcher, launcher, image editor, etc.), all written in C#.

The original codebase targets **Windows only**, using:
- **SharpDX** (Direct3D 9/11) for rendering
- **DirectSound** for audio
- **Windows Forms** for windowing, input, and embedded text controls
- **System.Drawing.Common** (GDI+) for text rasterization and light texture generation

## Goal of This Port

Make the game client and server run on **Linux** (and eventually macOS) using a single codebase with conditional compilation. Windows support is preserved — the original DirectX backends remain available on Windows.

### Technology choices

| Layer | Windows (original) | Linux (new) |
|-------|-------------------|-------------|
| Windowing | WinForms `RenderForm` | SDL3 |
| Rendering | Direct3D 9/11 (SharpDX) | OpenGL 3.3 |
| Audio | DirectSound (SharpDX) | SDL3 Audio (stub) |
| Text | GDI+ `TextRenderer` / `Graphics.DrawString` | SDL3_ttf (FreeType) |
| Input | WinForms events | SDL3 events |
| Font | "MS Sans Serif" (system) | Liberation Sans / Noto Sans (system TTF) |

## Repository Structure

```
Zircon/
├── Client/                     # Game client (multi-target: net8.0 + net8.0-windows8.0)
│   ├── Audio/                  # Cross-platform audio abstractions
│   │   ├── ISoundManager.cs    # Audio interface
│   │   ├── SDL3SoundManager.cs # SDL3 audio stub (needs SDL3_mixer)
│   │   ├── DXSoundManagerCompat.cs  # DXSoundManager API shim for Linux
│   │   └── SoundType.cs       # Extracted from DXSoundManager
│   ├── Controls/               # Game UI widget system (DXControl hierarchy)
│   ├── Envir/                  # Client environment (config, connection, sound)
│   │   ├── DXSound.cs          # [Windows only] DirectSound playback
│   │   └── DXSoundManager.cs   # [Windows only] DirectSound manager
│   ├── Models/                 # Client-side game models
│   ├── Platform/               # Cross-platform abstractions
│   │   ├── IGameWindow.cs      # Window interface
│   │   ├── GameInputEvents.cs  # Platform-agnostic input types
│   │   ├── TextEditState.cs    # Text editing state machine
│   │   ├── WinFormsCompat.cs   # WinForms type shims for Linux
│   │   └── SDL3/
│   │       ├── SDL3Native.cs   # SDL3 P/Invoke bindings
│   │       ├── SDL3GameWindow.cs   # IGameWindow via SDL3
│   │       ├── SDL3InputMapper.cs  # SDL3 scancode mapping
│   │       └── SDL3TTF.cs      # SDL3_ttf P/Invoke + font cache
│   ├── Rendering/              # Rendering pipeline abstraction
│   │   ├── IRenderingPipeline.cs       # Pipeline interface (~50 methods)
│   │   ├── RenderingPipelineManager.cs # Static dispatcher + factory
│   │   ├── RenderingPipelineContext.cs # Render target context (object, not Form)
│   │   ├── RenderingPipelineIds.cs     # "DirectX 9", "DirectX 11", "OpenGL"
│   │   ├── GameFont.cs         # Cross-platform font descriptor
│   │   ├── GameTextFormatFlags.cs  # Cross-platform text format flags
│   │   ├── ITextRenderer.cs    # Text renderer interface
│   │   ├── SharpDXD3D9/        # [Windows only] Direct3D 9 backend
│   │   ├── SharpDXD3D11/       # [Windows only] Direct3D 11 backend
│   │   └── SDL3OpenGL/         # Cross-platform OpenGL 3.3 backend
│   │       ├── SDL3OpenGLRenderingPipeline.cs  # IRenderingPipeline impl
│   │       ├── GLManager.cs    # Texture/FBO/resource management
│   │       ├── GLSpriteRenderer.cs  # Batched quad renderer
│   │       ├── GLShaderProgram.cs   # GLSL compiler
│   │       ├── GL.cs           # OpenGL function loader via SDL3
│   │       ├── LightGeneratorGL.cs  # Pure-math light texture
│   │       └── Shaders/
│   │           ├── Sprite.vert     # Shared vertex shader
│   │           ├── Sprite.frag     # Basic sprite
│   │           ├── Grayscale.frag  # Disabled-state effect
│   │           ├── DropShadow.frag # Window shadow
│   │           └── Outline.frag    # Selection outline
│   ├── Scenes/                 # Game scenes (Login, Select, Game)
│   ├── TargetForm.cs           # [Windows only] WinForms render window
│   └── Program.cs              # Entry point (#if WINDOWS conditional)
├── ServerCore/                 # Console-based headless server (net8.0)
├── ServerLibrary/              # Server game logic (net8.0)
├── LibraryCore/                # Shared code: networking, DB ORM, types (net8.0)
├── Server/                     # [Windows only] GUI admin tool (DevExpress)
└── docs/
```

## What Has Been Done

### Phase 0: Server on Linux
- Fixed all hardcoded `\` path separators → `/` in LibraryCore, ServerLibrary
- `System.Drawing.Point`/`Color`/`Size` primitives work cross-platform in .NET 8 out of the box
- ServerCore builds and runs on Linux, accepts TCP connections, loads database

### Phase 1: Platform Abstraction
- Multi-target `Client.csproj`: `net8.0` (Linux/macOS) + `net8.0-windows8.0` (Windows)
- `WINDOWS` define for conditional compilation
- `IRenderingPipeline` removed `Form`/`Font`/`Graphics`/`TextFormatFlags` dependencies
- `RenderingPipelineContext` takes `object` instead of `Form`
- `IGameWindow` interface with SDL3 and WinForms implementations
- `WinFormsCompat.cs` provides stub implementations of `System.Windows.Forms` types (Keys, MouseEventArgs, TextBox, Font, Graphics, TextRenderer, etc.)
- `GameFont` cross-platform font descriptor
- All P/Invoke `user32.dll` calls wrapped in `#if WINDOWS`

### Phase 2: SDL3+OpenGL Rendering
- `SDL3OpenGLRenderingPipeline` implements all ~50 `IRenderingPipeline` methods
- `GL.cs` loads OpenGL functions dynamically via `SDL_GL_GetProcAddress`
- `GLManager` handles texture creation (A8R8G8B8, DXT1, DXT5), FBOs, render targets
- `GLSpriteRenderer` draws textured quads with all 13 blend modes
- GLSL shaders ported from HLSL (Sprite, Grayscale, DropShadow, Outline)
- Vertex shader uses `vec4 * uMatrix` to match HLSL's `mul(vec4, Matrix)` convention
- Matrix transposed in C# matching D3D11's approach
- White pixel texture for untextured draws
- FBO Y-flip correction when drawing render target textures

### Phase 3: SDL3 Audio (Stub)
- `ISoundManager` interface
- `SDL3SoundManager` stub (initializes SDL audio, play/stop are no-ops)
- `DXSoundManagerCompat` provides the `DXSoundManager` static API on Linux
- `SoundType` enum extracted to shared file

### Phase 4: Text Rendering
- `SDL3TTF.cs` with P/Invoke bindings, font cache, system font resolution
- `TextRenderer.DrawText` implemented via `TTF_RenderText_Blended` with alpha compositing
- `TextRenderer.MeasureText` implemented via `TTF_GetStringSize`
- Font scaling factor (1.4x) to match "MS Sans Serif" visual size
- `Bitmap` and `Graphics` compat types store pixel buffer pointers for direct rendering
- System font search: Liberation Sans → Noto Sans → DejaVu Sans → `fc-match`

### Phase 5: Input Handling
- SDL3 event loop dispatches mouse/keyboard/text events to `DXControl.ActiveScene`
- Key mapping from SDL3 scancodes to `System.Windows.Forms.Keys` values
- Mouse click order: `OnMouseClick` before `OnMouseUp` (matches WinForms behavior where `FocusControl` must be valid during click)
- `SDL_EVENT_TEXT_INPUT` routed to active `DXTextBox`'s internal TextBox
- `SDL_EVENT_KEY_DOWN` routed to active TextBox for editing (backspace, delete, arrows, clipboard)
- Compat `TextBox.OnKeyDown`/`OnKeyPress` handle character insertion, cursor movement, selection, Ctrl+A/C/V/X

### Phase 6: Polish
- Cross-platform `Program.cs` with `#if WINDOWS` conditional
- File path normalization (all `@".\path\"` → `"./path/"`)
- `CConnection.Process(G.CheckVersion)` uses assembly location instead of `Application.ExecutablePath`
- `ServerConnected` flag set in `GoodVersion` handler
- `LoginBox` location recalculated after library images load
- `DXTextBox.UpdateDisplayArea` falls back to `base.Size` when `TextBox.Size` is zero

## Known Remaining Issues

### Critical (blocks gameplay)

1. **FBO render-to-texture content may be invisible** — The FBO Y-flip fix has been implemented but needs testing. Window frame borders, dialog boxes, and control textures rendered via FBOs may still appear blank. The root cause was that OpenGL FBO textures store content Y-flipped relative to the sampling UVs. The fix flips V coordinates when drawing FBO-backed textures.

2. **Audio not implemented** — `SDL3SoundManager` is a stub. Needs SDL3_mixer or SDL3_sound integration for WAV/MP3 playback. The system has `libSDL3_sound.so` installed.

3. **Text input in DXTextBox partially working** — Character input and basic editing (backspace, delete, arrows) work via the compat TextBox. But the visual rendering of text in text boxes depends on `TextBox.DrawToBitmap` which needs the pixel buffer to be correctly passed through. The `DrawToBitmap` implementation renders text via SDL3_ttf but may not work in all cases.

4. **66 missing library files** — 5 of 314 `.Zl` library files are not available in the mirfiles.com patch set. These are non-critical (store items, minimap2, some magic effects).

### Non-Critical

5. **Cursor changes** — `Cursor.Current = Cursors.IBeam` etc. are no-ops on Linux. Needs SDL3 cursor integration.

6. **Screenshot** — `TargetForm.CreateScreenShot` uses Win32 GDI. Needs `glReadPixels` implementation.

7. **Fullscreen/resolution switching** — `ToggleFullScreen` calls `SDL_SetWindowFullscreen` but hasn't been tested.

8. **Font metrics mismatch** — Liberation Sans metrics differ slightly from MS Sans Serif. Some UI layouts may have minor alignment issues. The 1.4x font scale helps but isn't pixel-perfect.

9. **DXTextBox rendering** — The embedded WinForms TextBox is replaced with a compat stub. Advanced features (IME, multi-line editing, text selection rendering) need work.

## Game Data Setup

### Required Files

The game needs matching client data and server database. Download from [mirfiles.com](https://mirfiles.com/resources/mir3/zircon/):

1. **Base client**: `Client.7z` — extract to get `Data/`, `Sound/`, `Map/` directories
2. **Patches**: `patch/` directory — contains updated `.Zl`, `.map`, and `.wav` files as `.gz` archives
3. **Database**: `Database.7z` — contains `System.db` for the server

### Applying Patches

The base `Client.7z` has outdated `.Zl` files. You MUST apply the patches:

```bash
# Download all patches
mkdir -p /tmp/zircon_patches && cd /tmp/zircon_patches
curl -sL "https://mirfiles.com/resources/mir3/zircon/patch/" | \
  grep -oP 'href="/resources/mir3/zircon/patch/[^"]*\.gz"' | \
  sed 's/href="//' | sed 's/"//' > patch_list.txt

cat patch_list.txt | xargs -P 10 -I{} sh -c \
  'curl -sL -A "Mozilla/5.0" -o "$(basename "{}")" "https://mirfiles.com{}"'

# Apply Data patches
for f in Data-*.gz; do
    outname=$(echo "$f" | sed 's/^Data-//' | sed 's/\.gz$//')
    gunzip -c "$f" > "/path/to/client_data/Data/$outname"
done

# Apply Map patches
for f in Map-*.gz; do
    outname=$(echo "$f" | sed 's/^Map-//' | sed 's/\.gz$//')
    gunzip -c "$f" > "/path/to/client_data/Map/$outname"
done

# Apply Sound patches
for f in Sound-*.gz; do
    outname=$(echo "$f" | sed 's/^Sound-//' | sed 's/\.gz$//')
    gunzip -c "$f" > "/path/to/client_data/Sound/$outname"
done
```

### Database Setup

The server database (`System.db`) must match the client data version. Copy it from `Database.7z`:

```bash
# For the client (loads game definitions)
cp Database/System.db /path/to/client_data/Data/System.db

# For the server
mkdir -p ServerCore/bin/Release/Database
cp Database/System.db ServerCore/bin/Release/Database/System.db
```

## Building

### Prerequisites

- .NET 8.0 SDK
- SDL3 (`libSDL3.so`) — windowing, input, GL context
- SDL3_ttf (`libSDL3_ttf.so`) — font rendering
- A system TTF font (Liberation Sans, Noto Sans, or DejaVu Sans)

On Fedora:
```bash
sudo dnf install dotnet-sdk-8.0 SDL3-devel SDL3_ttf-devel liberation-sans-fonts
```

### Build Commands

```bash
# Server (headless, console)
dotnet build ServerCore/ServerCore.csproj -c Release

# Client (cross-platform, SDL3+OpenGL)
dotnet build Client/Client.csproj -f net8.0 -c Release

# Client (Windows, DirectX) — only on Windows
dotnet build Client/Client.csproj -f net8.0-windows8.0 -c Release
```

### Output Locations

- Server: `ServerCore/bin/Release/`
- Client: `Debug/Client/` (custom output path in csproj)

## Testing

### Server

```bash
cd ServerCore/bin/Release

# Create server config (optional)
cat > Server.ini << 'EOF'
[System]
CheckVersion=False
TestServer=True
EOF

# Symlink data
ln -sf /path/to/client_data/Map Map
mkdir -p Database
cp /path/to/Database/System.db Database/System.db

# Run
dotnet ServerCore.dll
# Should print: "Network Started. Listen: 127.0.0.1:7000"
```

### Client

```bash
cd Debug/Client

# Symlink game data
ln -sf /path/to/client_data/Data Data
ln -sf /path/to/client_data/Sound Sound
ln -sf /path/to/client_data/Map Map

# Copy database (must match server)
cp /path/to/Database/System.db Data/System.db

# Run (server must be running first)
dotnet Zircon.dll
```

### What to Expect

1. SDL3 window opens with game background
2. Client connects to server on `127.0.0.1:7000`
3. "Loading client information" dialog appears briefly
4. Login form appears at bottom of screen
5. Clicking "New Account" opens account creation dialog
6. Text rendering visible (labels, buttons, dialog text)

### Verifying the Rendering Pipeline

The OpenGL pipeline registers as `"OpenGL"` in `RenderingPipelineIds`. On Linux, this is the default and only pipeline. To verify:
- Background images (DXT1 textures from `.Zl` files) should render correctly
- UI controls using FBO render targets should be visible (after Y-flip fix)
- Text labels render via SDL3_ttf with Liberation Sans font

## Architecture Notes for Developers

### Rendering Pipeline Design

The existing `IRenderingPipeline` interface is well-abstracted. The `RenderingPipelineManager` uses a factory pattern with runtime pipeline selection. Adding a new backend (e.g., Vulkan) requires:

1. Create a new directory under `Client/Rendering/`
2. Implement `IRenderingPipeline` (~50 methods)
3. Register in `RenderingPipelineManager`'s static dictionary

### Conditional Compilation

The `WINDOWS` define is set in `Client.csproj` for the `net8.0-windows8.0` target. Use `#if WINDOWS` to guard Windows-specific code. The `WinFormsCompat.cs` file provides the `System.Windows.Forms` namespace on non-Windows platforms.

### DXControl System

The game's UI is NOT WinForms — it's a custom widget system (`DXControl` hierarchy) that renders via the pipeline. Controls manage their own textures:
- `DXControl.CreateTexture()` creates an FBO, clears it, calls `OnClearTexture()`
- `DXImageControl.DrawMirTexture()` draws library images directly
- `DXLabel.CreateTexture()` renders text into a locked A8R8G8B8 texture
- `DXWindow.DrawWindow()` renders frame parts into a full-screen FBO

### Texture Formats

- **DXT1** (BC1): 4-bit indexed color, 1-bit alpha. Used by version 0 `.Zl` files. Uploaded via `glCompressedTexImage2D`.
- **DXT5** (BC3): 8-bit indexed color, interpolated alpha. Used by version 1+ `.Zl` files.
- **A8R8G8B8**: 32-bit BGRA. Used for CPU-rendered textures (labels, text boxes). Uploaded via `glTexSubImage2D` with `GL_BGRA` format.

### FBO Y-Flip

OpenGL FBO textures store content with Y=0 at the bottom. When the game renders into an FBO using top-down coordinates (Y=0 at top, matching Direct3D convention), the result is stored upside-down. When sampling these textures, the V coordinates must be flipped: `v1 = 1 - v2_original`, `v2 = 1 - v1_original`.

### SDL3 API Notes

SDL3 (not SDL2) is used. Key API differences from SDL2:
- `SDL_Init` returns `bool`, not `int`
- `SDL_PollEvent` returns `bool`
- `SDL_GL_CreateContext` / `SDL_GL_DestroyContext` (not DeleteContext)
- Window event types renumbered: `SDL_EVENT_WINDOW_CLOSE_REQUESTED = 0x210` (not 0x202)
- `TTF_OpenFont` takes `float ptsize` (not int)
- `TTF_RenderText_Blended` signature: `(font, text, length, fg_color)`
- Event struct `SDL_Event` is 128 bytes with union layout

### Key Files for Debugging

| File | What it does |
|------|-------------|
| `SDL3OpenGLRenderingPipeline.cs` | Main pipeline — event loop, DrawTexture dispatch |
| `GLSpriteRenderer.cs` | Quad drawing, blend modes, shader selection |
| `GLManager.cs` | Texture/FBO creation, surface switching |
| `WinFormsCompat.cs` | All WinForms type stubs — TextRenderer, Font, TextBox, etc. |
| `SDL3TTF.cs` | Font loading, caching, text measurement |
| `DXControl.cs` | UI widget base — Draw(), CreateTexture(), PresentTexture() |
| `DXWindow.cs` | Window frame rendering into FBOs |
| `DXImageControl.cs` | Library image drawing |
| `DXTextBox.cs` | Text input control (P/Invoke wrapped in #if WINDOWS) |

## Commit History

1. `9aa939c` — Initial cross-platform port: SDL3+OpenGL pipeline, multi-targeting, WinForms compat, GLSL shaders, SDL3_ttf text, path fixes
2. `0f046fb` — P/Invoke crash fixes, mouse click dispatch, text input routing, FBO fixes
