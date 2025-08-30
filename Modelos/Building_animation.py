import bpy
import random

# ==========================
# Parámetros
# ==========================
start_index   = 6     # primer bloque
end_index     = 80    # último bloque
start_frame   = 1     # frame inicial
offset_z      = 1.0   # altura desde donde "caen" los bloques
delay_frames  = 3     # frames de diferencia entre bloque y bloque
fall_duration = 8     # duración de la caída
random_rot    = True  # si rotan aleatoriamente al caer
action_name   = "building_improved"
armature_name = "Armature"

# ==========================
# Obtener Armature
# ==========================
armature = bpy.data.objects.get(armature_name)
if armature is None or armature.type != 'ARMATURE':
    raise Exception(f"❌ No se encontró el armature '{armature_name}'")

# ==========================
# Crear Action
# ==========================
action = bpy.data.actions.get(action_name) or bpy.data.actions.new(action_name)
armature.animation_data_create()
armature.animation_data.action = action

# ==========================
# Animación huesos
# ==========================
for i, obj_index in enumerate(range(start_index, end_index+1)):
    bone_name = f"Bloque_{obj_index}"
    if bone_name not in armature.pose.bones:
        print(f"⚠ No existe el hueso {bone_name}")
        continue

    pbone = armature.pose.bones[bone_name]

    # Guardamos posición y rotación originales
    orig_loc = pbone.location.copy()
    orig_rot = pbone.rotation_quaternion.copy()
    pbone.rotation_mode = 'QUATERNION'

    # Frames específicos para este bloque
    f_start = start_frame + i * delay_frames
    f_end   = f_start + fall_duration

    # 1) Inicio → bloque arriba y con rotación random
    pbone.location = (orig_loc.x, orig_loc.y, orig_loc.z + offset_z)
    if random_rot:
        pbone.rotation_quaternion = (
            random.uniform(-1, 1),
            random.uniform(-1, 1),
            random.uniform(-1, 1),
            random.uniform(-1, 1)
        )
        pbone.rotation_quaternion.normalize()

    pbone.keyframe_insert("location", frame=f_start)
    pbone.keyframe_insert("rotation_quaternion", frame=f_start)

    # 2) Fin → bloque en su posición original
    pbone.location = orig_loc
    pbone.rotation_quaternion = orig_rot
    pbone.keyframe_insert("location", frame=f_end)
    pbone.keyframe_insert("rotation_quaternion", frame=f_end)

print("✅ Animación 'building_improved' creada con caída, delay y rotaciones aleatorias.")
