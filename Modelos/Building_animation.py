import bpy

# --------------------------
# Parámetros
# --------------------------
start_index = 5   # primer bloque
end_index   = 80  # último bloque
start_frame = 1   # frame inicial
offset_z    = 1.0 # posición en Z temporal

# --------------------------
# Armature
# --------------------------
armature_name = "Armature"  # Cambia si tu armature tiene otro nombre
armature = bpy.data.objects.get(armature_name)
if armature is None or armature.type != 'ARMATURE':
    raise Exception("❌ No se encontró el armature llamado 'Armature'")

# --------------------------
# Crear Action
# --------------------------
action_name = "building"
if action_name in bpy.data.actions:
    action = bpy.data.actions[action_name]
else:
    action = bpy.data.actions.new(action_name)

armature.animation_data_create()
armature.animation_data.action = action

# --------------------------
# Animación huesos
# --------------------------
for i, obj_index in enumerate(range(start_index, end_index+1)):
    bone_name = f"Bloque_{obj_index}"  # hueso con mismo nombre que bloque
    if bone_name not in armature.pose.bones:
        print(f"⚠ No existe el hueso {bone_name}")
        continue
    
    pbone = armature.pose.bones[bone_name]
    
    # Guardamos posición original (en espacio local del hueso)
    original_loc = pbone.location.copy()
    
    # Frame donde empieza el bloque a volver
    f = start_frame + i
    
    # 1) En frame inicial → mover hueso a offset
    pbone.location = (original_loc.x, original_loc.y, offset_z)
    pbone.keyframe_insert(data_path="location", frame=start_frame)
    
    # 2) En frame correspondiente → devolver a posición original
    pbone.location = original_loc
    pbone.keyframe_insert(data_path="location", frame=f)

print("✅ Animación 'building' creada en huesos.")