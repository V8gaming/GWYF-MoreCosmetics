# GWYF New Clothing — Blender Export Script
# Workflow:
#   1. File > Import > FBX, select the mesh
#   2. Run this script — bakes atlas to new UVs, exports OBJ + PNG
#   3. Toggle UVs: Object Data Properties > UV Maps (AtlasUV vs BakeUV)
#   4. Toggle shader: Shading tab > Mix Factor (0=atlas, 1=paint)
# Log: bake_log.txt (overwritten each run)

import bpy
import os
from datetime import datetime

OUTPUT_SIZE = 1024
LOG_LINES = []


def log(msg):
    print(msg)
    LOG_LINES.append(msg)


def find_script_dir():
    for text in bpy.data.texts:
        if text.filepath and os.path.isfile(text.filepath):
            name = os.path.basename(text.filepath)
            if name.endswith('.py') and 'bake' in name.lower():
                return os.path.dirname(text.filepath)
    if bpy.data.filepath:
        return os.path.dirname(bpy.data.filepath)
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except (NameError, OSError):
        pass
    return None


def write_log_file(script_dir):
    path = os.path.join(script_dir, "bake_log.txt")
    try:
        with open(path, 'w') as f:
            f.write(f"GWYF Bake Log — {datetime.now()}\n{'=' * 50}\n")
            for line in LOG_LINES:
                f.write(line + "\n")
    except Exception as ex:
        log(f"  (could not write log: {ex})")


def main():
    script_dir = find_script_dir()
    if script_dir is None:
        log("ERROR: Could not determine script location. Save the .blend first.")
        return

    atlas_path = os.path.join(script_dir, "Casino_MasterTexture.png")
    output_dir = os.path.join(script_dir, "baked")

    log("\n" + "=" * 50)
    log(f"  GWYF Export Script  —  {datetime.now().strftime('%H:%M:%S')}")
    log(f"  Dir   : {script_dir}")
    log(f"  Atlas : {'FOUND' if os.path.isfile(atlas_path) else 'MISSING'}")
    log("=" * 50)

    obj = bpy.context.active_object
    if obj is None or obj.type != 'MESH':
        log("ERROR: No mesh selected.")
        write_log_file(script_dir)
        return

    log(f"\nMesh: '{obj.name}' ({len(obj.data.vertices)}v, {len(obj.data.polygons)}f)")

    # === 1. UV Maps (create BEFORE material so UV Map nodes can reference them) ===
    log("\n[1/6] UV maps...")
    # Rename first UV to AtlasUV
    if len(obj.data.uv_layers) > 0:
        obj.data.uv_layers[0].name = "AtlasUV"
        log("  Renamed first UV -> 'AtlasUV'")
    else:
        obj.data.uv_layers.new(name="AtlasUV")
        log("  Created 'AtlasUV'")

    # Remove stray UV maps (keep only AtlasUV)
    to_keep = "AtlasUV"
    for uv in list(obj.data.uv_layers):
        if uv.name != to_keep:
            obj.data.uv_layers.remove(uv)
            log(f"  Removed extra UV: '{uv.name}'")

    # Create bake UV map
    obj.data.uv_layers.new(name="BakeUV")
    obj.data.uv_layers.active = obj.data.uv_layers['BakeUV']
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=66, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    log("  Created 'BakeUV' (Smart UV Project)")
    log(f"  UV maps: {[u.name for u in obj.data.uv_layers]}")

    # === 2. Material with UV Map nodes ===
    log("\n[2/6] Material (UV Map nodes)...")
    mat = bpy.data.materials.new(name="GWYF_Mat")
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new('ShaderNodeOutputMaterial');  out.location = (1000, 200)
    bsdf = nodes.new('ShaderNodeBsdfPrincipled'); bsdf.location = (700, 200)
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])

    # UV Map nodes — explicit per-texture, created AFTER UV maps exist
    uv_atlas = nodes.new('ShaderNodeUVMap')
    uv_atlas.location = (-250, 350)
    uv_atlas.uv_map = "AtlasUV"
    uv_atlas.name = "UV_Atlas"
    log(f"  UV Map node: 'AtlasUV'")

    uv_paint = nodes.new('ShaderNodeUVMap')
    uv_paint.location = (-250, 100)
    uv_paint.uv_map = "BakeUV"  # will be renamed to UVMap later
    uv_paint.name = "UV_Paint"
    log(f"  UV Map node: 'BakeUV'")

    # Atlas texture (source)
    atlas_node = nodes.new('ShaderNodeTexImage')
    atlas_node.location = (0, 350)
    atlas_node.name = "AtlasRef"; atlas_node.label = "Atlas (src)"
    if os.path.isfile(atlas_path):
        try:
            atlas_node.image = bpy.data.images.load(atlas_path)
            log(f"  Loaded atlas: {atlas_node.image.size[0]}x{atlas_node.image.size[1]}")
        except Exception as ex:
            log(f"  Atlas load error: {ex}")

    # Paint texture (bake destination, then editable)
    paint_node = nodes.new('ShaderNodeTexImage')
    paint_node.location = (0, 100)
    paint_node.name = "PaintTex"; paint_node.label = "Paint (dst)"
    dest_img = bpy.data.images.new("BakedResult", OUTPUT_SIZE, OUTPUT_SIZE, alpha=False)
    dest_img.generated_color = (0.2, 0.2, 0.2, 1)
    paint_node.image = dest_img
    log(f"  Created {OUTPUT_SIZE}x{OUTPUT_SIZE} image")

    # Connect UV Maps -> Textures
    links.new(uv_atlas.outputs['UV'], atlas_node.inputs['Vector'])
    links.new(uv_paint.outputs['UV'], paint_node.inputs['Vector'])

    # Mix node (default 0 = atlas visible for baking)
    mix = nodes.new('ShaderNodeMix'); mix.location = (400, 200)
    mix.data_type = 'RGBA'
    mix.inputs['Factor'].default_value = 0.0  # 0=atlas, 1=paint
    links.new(atlas_node.outputs['Color'], mix.inputs['A'])
    links.new(paint_node.outputs['Color'], mix.inputs['B'])
    links.new(mix.outputs['Result'], bsdf.inputs['Base Color'])
    log("  Mix Factor=0 (atlas visible)")

    # === 3. Select bake target ===
    log("\n[3/6] Bake target...")
    for node in nodes:
        node.select = False
    paint_node.select = True
    nodes.active = paint_node
    log("  Selected 'Paint (dst)' as bake target")

    # === 4. Bake ===
    log("\n[4/6] Baking...")
    old_engine = bpy.context.scene.render.engine
    bpy.ops.object.mode_set(mode='OBJECT')

    try:
        bpy.context.scene.render.engine = 'CYCLES'
        # Re-select object (may have been deselected by mode switches)
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        log(f"  Object selected: {obj.name}")

        bpy.context.scene.cycles.samples = 4

        # Use BakeUV as active — the UV Map node on AtlasRef routes AtlasUV
        # coordinates to the atlas texture, so it samples correctly regardless.
        obj.data.uv_layers.active = obj.data.uv_layers['BakeUV']
        for uv in obj.data.uv_layers:
            uv.active_render = (uv.name == "BakeUV")
        log(f"  Active UV: BakeUV (destination layout)")

        scene = bpy.context.scene

        bake_type_set = False
        try:
            scene.cycles.bake_type = 'DIFFUSE'
            bake_type_set = True
            log(f"  Bake type: DIFFUSE (via scene.cycles.bake_type)")
        except (AttributeError, TypeError):
            pass
        if not bake_type_set:
            try:
                scene.render.bake.type = 'DIFFUSE'
                bake_type_set = True
                log(f"  Bake type: DIFFUSE (via scene.render.bake.type)")
            except TypeError:
                pass

        if not bake_type_set:
            log("  ERROR: Could not set DIFFUSE bake type — skipping.")
        else:
            scene.render.bake.target = 'IMAGE_TEXTURES'
            scene.render.bake.use_pass_direct = False
            scene.render.bake.use_pass_indirect = False
            scene.render.bake.use_pass_color = True

            try:
                bpy.ops.object.bake(type='DIFFUSE')
                log("  Bake OK!")
            except TypeError:
                bpy.ops.object.bake()
                log("  Bake OK! (no type param)")
    finally:
        bpy.context.scene.render.engine = old_engine
        log("  Engine restored.")

    # === 5. Finalize ===
    log("\n[5/6] Finalizing...")
    paint_node.image = dest_img

    # Look up BakeUV by name (variable may be stale after mode switches)
    bake_uv_layer = obj.data.uv_layers.get('BakeUV')
    if bake_uv_layer is not None:
        bake_uv_layer.name = "UVMap"
        uv_paint.uv_map = "UVMap"
        log("  Renamed BakeUV -> UVMap")
    else:
        log("  WARNING: BakeUV layer not found, keeping current names.")

    obj.data.uv_layers.active = obj.data.uv_layers.get('UVMap', obj.data.uv_layers[0])
    mix.inputs['Factor'].default_value = 1.0
    log(f"  UV final: {[u.name for u in obj.data.uv_layers]}")
    log(f"  Active: {obj.data.uv_layers.active.name}, Mix=1 (paint)")

    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces[0].shading.type = 'MATERIAL'
            break

    # === 6. Export ===
    log("\n[6/6] Exporting...")
    os.makedirs(output_dir, exist_ok=True)
    safe_name = obj.name.replace('.', '_')
    obj_path = os.path.join(output_dir, f"{safe_name}.obj")
    png_path = os.path.join(output_dir, f"{safe_name}.png")

    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.ops.wm.obj_export(
        filepath=obj_path,
        export_selected_objects=True,
        forward_axis='NEGATIVE_Z',
        up_axis='Y',
        export_materials=False
    )
    dest_img.save_render(png_path)

    log(f"\n{'=' * 50}")
    log(f"OBJ : {obj_path}")
    log(f"PNG : {png_path}")
    log(f"")
    log(f"  Mix Factor: 0=atlas (AtlasUV)  |  1=paint (UVMap)")
    log(f"  UV Maps: AtlasUV (original)  |  UVMap (new, exported)")
    log(f"")
    log(f"  Copy OBJ -> models/{safe_name}.obj")
    log(f"  Copy PNG -> textures/{safe_name}.png")
    log(f"")
    log(f'{{ "name": "{safe_name}",')
    log(f'  "type": "Clothing",')
    log(f'  "model": {{ "obj": "{safe_name}.obj" }},')
    log(f'  "texture": "{safe_name}.png" }}')
    log(f"{'=' * 50}\n")

    write_log_file(script_dir)
    log(f"Log: {os.path.join(script_dir, 'bake_log.txt')}")


if __name__ == "__main__":
    try:
        main()
    except Exception as ex:
        log(f"\nFATAL ERROR: {ex}")
        import traceback
        traceback.print_exc()
        LOG_LINES.append(traceback.format_exc())
        try:
            script_dir = find_script_dir()
            if script_dir:
                write_log_file(script_dir)
        except:
            pass
