# GWYF New Clothing — Blender Tools

## Quick Start (Script Method)

1. **Export FBX from Unity**  
   - Open the AssetRipper-exported project in Unity 6000  
   - Find the cosmetic prefab in `Assets/GameObject/`  
   - Double-click it, select the root GameObject in the **Hierarchy**  
   - Right-click → **Export To FBX...** → save

2. **Import & run script**  
   - In Blender: **File → New → Import → FBX** → select your FBX  
   - Click the imported mesh in the viewport (orange outline)  
   - Switch to **Scripting** workspace → **Open** → `tools/blender_bake.py`  
   - Click **Run Script** (play button ▸)

3. **Paint & export**  
   - Switch to **Texture Paint** workspace → paint your design  
   - **Image → Save As** → overwrite `tools/baked/YourMesh.png`  
   - Copy OBJ → `models/`, PNG → `textures/`

4. **Re-use**  
   - Delete the old object from the scene (select → X)  
   - File → Import → FBX → select FBX again → run script again

## Manual Method (No Script)

### Setup
1. Import FBX  
2. In the **Shading** workspace (or Shader Editor):  
   - Add a **UV Map node** (Shift+A → Input → UV Map), set UV Map to the original UV layer  
   - Add an **Image Texture** node, load `Casino_MasterTexture.png`  
   - Connect UV Map → Image Texture(Vector), Image Texture(Color) → Principled BSDF(Base Color)  
3. Create a second UV map: **Object Data Properties** (green triangle) → UV Maps → **+**  
   - Name it `BakeUV`  
   - Select it, enter Edit Mode, select all (A), **UV → Smart UV Project**  

### Baking
4. **Before baking, ensure these 3 things are selected:**  
   - **Object**: the mesh must be selected in the **3D viewport** (orange outline)  
   - **UV map**: the **original** UV map must be active in Object Data → UV Maps (click its name)  
   - **Image texture node**: the **floating/unconnected** image node in the Shader Editor must be selected (white outline) — this is the bake destination  

5. Render Properties → Render Engine → **Cycles**  
6. Scroll to Bake section → **Bake Type: Diffuse** → uncheck Direct + Indirect  
7. Target: **Image Textures**  
8. Click **Bake**  

### After Baking
9. Switch active UV map to `BakeUV`  
10. Connect the baked image node to Principled BSDF  
11. Switch viewport to **Material Preview** to see the result  
12. Paint or export

## Unity Setup (One-Time)

### Installing FBX Exporter
- Window → Package Manager → dropdown: **Unity Registry**  
- Search "FBX" → Install **FBX Exporter** (Unity Technologies)

### Fixing Timeline Duplicate Error
If you see: `Assembly 'Unity.Timeline' already exists`  
- Delete `Assets/Scripts/Unity.Timeline/` (the AssetRipper duplicate)  
- Repeat for any other `Assets/Scripts/Unity.*` folders that conflict  

### Exporting a Cosmetic
- In the Project window, find `Assets/GameObject/Shirt_1.prefab` (or whichever)  
- Double-click to open Prefab Mode  
- Select the **root GameObject in the Hierarchy**  
- Right-click → **Export To FBX...** (leave default settings)  

## File Layout
```
tools/
├── blender_bake.py          ← Run this in Blender
├── Casino_MasterTexture.png ← Atlas (place here before running script)
├── baked/                   ← Output OBJ + PNG (auto-created)
│   └── YourMesh.obj/.png
├── bake_log.txt             ← Log from last script run (debug)
└── README.md                ← This file

(Optionally place FBX files here too)
```

## JSON Entry
After copying OBJ and PNG to the mod folders, add to `cosmetics.json`:
```json
{ "name": "My Cosmetic", "type": "Clothing",
  "model": { "obj": "YourMesh.obj" },
  "texture": "YourMesh.png" }
```
