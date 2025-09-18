import bpy

# Cambia esto por el nombre de tu armature principal
ARMATURE_NAME = "Armature"

armature = bpy.data.objects.get(ARMATURE_NAME)

if armature is None:
    raise ValueError(f"No se encontró un Armature llamado '{ARMATURE_NAME}'")

for obj in bpy.data.objects:
    if obj.type == "MESH" and obj.parent_type == 'BONE':
        bone_name = obj.parent_bone
        print(f"Procesando {obj.name} (hijo de {bone_name})")

        # Quitar parent directo al hueso
        obj.parent_type = 'OBJECT'
        obj.parent = armature

        # Añadir Armature Modifier si no existe
        if not any(mod.type == 'ARMATURE' for mod in obj.modifiers):
            mod = obj.modifiers.new(name="Armature", type='ARMATURE')
            mod.object = armature

        # Crear vertex group si no existe
        if bone_name not in obj.vertex_groups:
            vgroup = obj.vertex_groups.new(name=bone_name)
        else:
            vgroup = obj.vertex_groups[bone_name]

        # Asignar todos los vértices a ese grupo con peso 1
        mesh = obj.data
        verts = [v.index for v in mesh.vertices]
        vgroup.add(verts, 1.0, 'REPLACE')
