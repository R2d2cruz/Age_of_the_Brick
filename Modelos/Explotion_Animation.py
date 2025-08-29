import bpy
import random
import math
from mathutils import Vector

# --------------------------
# Parámetros
# --------------------------
fixed_index     = 19   # bloques que se quedan quietos (del 1 al fixed_index)
start_frame     = 1    # frame inicial
explosion_frame = 10   # frame en el que empieza la explosión
fall_frame      = 30   # frame donde los bloques caen
strength        = 0.05  # qué tan lejos salen volando
rot_strength    = math.radians(720)  # hasta cuántos grados giran (en radianes)

# --------------------------
# Armature
# --------------------------
armature_name = "Armature"  # cambia si tu armature se llama distinto
armature = bpy.data.objects.get(armature_name)
if armature is None or armature.type != 'ARMATURE':
    raise Exception("❌ No se encontró el armature llamado 'Armature'")

# --------------------------
# Crear Action
# --------------------------
action_name = "explosion_fall"
if action_name in bpy.data.actions:
    action = bpy.data.actions[action_name]
else:
    action = bpy.data.actions.new(action_name)

armature.animation_data_create()
armature.animation_data.action = action

# --------------------------
# Animación huesos
# --------------------------
for obj_index, pbone in enumerate(armature.pose.bones, start=1):
    bone_name = pbone.name
    
    # Guardamos posición y rotación original
    original_loc = pbone.location.copy()
    original_rot = pbone.rotation_euler.copy()
    
    # --- Bloques fijos ---
    if obj_index <= fixed_index:
        pbone.location = original_loc
        pbone.rotation_euler = original_rot
        pbone.keyframe_insert(data_path="location", frame=start_frame)
        pbone.keyframe_insert(data_path="location", frame=fall_frame)
        pbone.keyframe_insert(data_path="rotation_euler", frame=start_frame)
        pbone.keyframe_insert(data_path="rotation_euler", frame=fall_frame)
        continue
    
    # --- Bloques que explotan ---
    # Frame inicial (quietos)
    pbone.location = original_loc
    pbone.rotation_euler = original_rot
    pbone.keyframe_insert(data_path="location", frame=start_frame)
    pbone.keyframe_insert(data_path="rotation_euler", frame=start_frame)
    
    # Frame de explosión
    dx = (random.random() - 0.5) * 2 * strength
    dy = (random.random() - 0.5) * 2 * strength
    dz = random.random() * strength * 2  # más fuerza hacia arriba
    
    explosion_loc = original_loc + Vector((dx, dy, dz))
    
    rx = (random.random() - 0.5) * rot_strength
    ry = (random.random() - 0.5) * rot_strength
    rz = (random.random() - 0.5) * rot_strength
    explosion_rot = (
        original_rot.x + rx,
        original_rot.y + ry,
        original_rot.z + rz
    )
    
    pbone.location = explosion_loc
    pbone.rotation_euler = explosion_rot
    pbone.keyframe_insert(data_path="location", frame=explosion_frame)
    pbone.keyframe_insert(data_path="rotation_euler", frame=explosion_frame)
    
    # Frame de caída (regresan al suelo, pero no necesariamente a su sitio original)
    fall_loc = Vector((explosion_loc.x, explosion_loc.y, original_loc.z))  # mismo X/Y, en Z caen
    fall_rot = explosion_rot  # se quedan girados
    
    pbone.location = fall_loc
    pbone.rotation_euler = fall_rot
    pbone.keyframe_insert(data_path="location", frame=fall_frame)
    pbone.keyframe_insert(data_path="rotation_euler", frame=fall_frame)

print("✅ Animación 'explosion_fall' creada.")
