import bpy
from mathutils import Vector

# === Configuración ===
nombre_base = "Bloque"
armature_name = "Armature"
longitud_hueso = 0.008  # 0.008 m hacia +X

collection = bpy.context.collection

# 1) Aplicar transformaciones
for obj in collection.objects:
    if obj.type == 'MESH':
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# 2) Crear o recuperar Armature
if armature_name not in bpy.data.objects:
    bpy.ops.object.armature_add(enter_editmode=False, location=(0,0,0))
    armature = bpy.context.object
    armature.name = armature_name
else:
    armature = bpy.data.objects[armature_name]

if armature.name not in {o.name for o in collection.objects}:
    collection.objects.link(armature)

# 3) Crear huesos en EDIT MODE
bpy.context.view_layer.objects.active = armature
bpy.ops.object.mode_set(mode='EDIT')
arm = armature.data

# Crear hueso base
if "Hueso_Base" not in arm.edit_bones:
    bone_base = arm.edit_bones.new("Hueso_Base")
    bone_base.head = Vector((0,0,0))
    bone_base.tail = Vector((0,0,longitud_hueso))
else:
    bone_base = arm.edit_bones["Hueso_Base"]

# Crear huesos para cada mesh
contador = 1
for obj in collection.objects:
    if obj.type != 'MESH':
        continue
    nuevo_nombre = f"{nombre_base}_{contador}"
    obj.name = nuevo_nombre

    loc = obj.matrix_world.translation

    if nuevo_nombre not in arm.edit_bones:
        eb = arm.edit_bones.new(nuevo_nombre)
    else:
        eb = arm.edit_bones[nuevo_nombre]

    eb.head = loc
    eb.tail = loc + Vector((0,longitud_hueso,0))
    eb.parent = bone_base
    contador += 1

bpy.ops.object.mode_set(mode='OBJECT')

# 4) Parenting directo → huesos
for obj in collection.objects:
    if obj.type == 'MESH' and obj.name in arm.bones:
        obj.matrix_world.translation = Vector((0,-longitud_hueso,0))
        obj.parent = armature
        obj.parent_type = 'BONE'
        obj.parent_bone = obj.name
        obj.matrix_parent_inverse.identity()

print("✅ Huesos creados y centrados en cada malla sin mover el modelo.")
