import bpy

# 🔹 Ajusta estos valores según tu archivo
ARMATURE_NAME = "Armature"
MAIN_ACTION = "Idle"  # Nombre exacto de tu animación Idle

# Buscar el armature
armature = bpy.data.objects.get(ARMATURE_NAME)
if armature is None:
    raise Exception(f"No encontré el objeto '{ARMATURE_NAME}'")

if armature.animation_data is None:
    armature.animation_data_create()

# 🔹 Borrar todos los tracks previos para evitar conflictos
for track in list(armature.animation_data.nla_tracks):
    armature.animation_data.nla_tracks.remove(track)
print("🧹 Tracks previos eliminados.")

# 🔹 Crear un track nuevo por cada acción
for action in bpy.data.actions:
    action.use_fake_user = True
    start, end = action.frame_range

    track = armature.animation_data.nla_tracks.new()
    track.name = f"Track_{action.name}"
    strip = track.strips.new(action.name, int(start), action)
    strip.name = action.name
    print(f"✅ Acción '{action.name}' añadida en track '{track.name}'")

# 🔹 Establecer Idle como la animación principal
if MAIN_ACTION in bpy.data.actions:
    armature.animation_data.action = bpy.data.actions[MAIN_ACTION]
    print(f"🎬 '{MAIN_ACTION}' se ha establecido como animación principal (acción activa en el Armature).")
else:
    print("⚠️ No encontré la acción Idle, revisa el nombre exacto.")
