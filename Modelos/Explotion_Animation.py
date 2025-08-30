import bpy
import random
from mathutils import Vector, Quaternion

# ==========================
# Parámetros
# ==========================
fixed_index     = 19      # huesos 1..fixed_index se quedan quietos
start_frame     = 1
explosion_frame = 30
fall_frame      = 50
strength        = 0.05    # distancia radial del "salto"
ARMATURE_NAME   = "Armature"
random_angle    = 1.7     # magnitud del giro aleatorio (radianes aprox.)
fall_angle_max  = 3.14    # rotación extra dramática (~180°) al caer

# Rebotes
num_bounces     = 2       # cuántos rebotes pequeños
bounce_height   = 0.04    # altura máxima del primer rebote
bounce_decay    = 0.5     # cada rebote es más pequeño
bounce_spacing  = 10       # frames entre rebotes

# ==========================
# Obtener armature
# ==========================
armature = bpy.data.objects.get(ARMATURE_NAME)
if not armature or armature.type != 'ARMATURE':
    raise Exception(f"No se encontró armature '{ARMATURE_NAME}'")

action_name = "Explosion"
action = bpy.data.actions.get(action_name) or bpy.data.actions.new(action_name)
armature.animation_data_create()
armature.animation_data.action = action

# Epicentro en ORIGEN MUNDIAL
origin_world = Vector((0.0, 0.0, 0.0))
origin_arm   = armature.matrix_world.inverted() @ origin_world

# Suelo global
positions_world = [armature.matrix_world @ pb.bone.head_local for pb in armature.pose.bones]
min_z_world = min(v.z for v in positions_world)

# ==========================
# Utilidad
# ==========================
def quat_align_restZ_to_dir(pbone, dir_arm):
    rest_mat = pbone.bone.matrix_local.to_3x3()
    rest_z   = (rest_mat @ Vector((0,0,1))).normalized()
    return rest_z.rotation_difference(dir_arm)

# ==========================
# Animación
# ==========================
for idx, pbone in enumerate(armature.pose.bones, start=1):
    orig_loc = pbone.location.copy()
    pos_arm = pbone.matrix.translation
    pos_world = armature.matrix_world @ pos_arm  

    if idx <= fixed_index:
        pbone.location = orig_loc
        pbone.rotation_mode = 'QUATERNION'
        pbone.keyframe_insert("location", frame=start_frame)
        pbone.keyframe_insert("location", frame=fall_frame)
        pbone.keyframe_insert("rotation_quaternion", frame=start_frame)
        pbone.keyframe_insert("rotation_quaternion", frame=fall_frame)
        continue

    dir_arm = (pos_arm - origin_arm)
    if dir_arm.length == 0:
        dir_arm = Vector((0,0,1))
    dir_arm.normalize()

    # === Frame inicial
    pbone.location = orig_loc
    pbone.rotation_mode = 'QUATERNION'
    pbone.keyframe_insert("location", frame=start_frame)
    pbone.keyframe_insert("rotation_quaternion", frame=start_frame)

    # === Frame explosión
    explosion_loc = orig_loc + dir_arm * strength
    q_align = quat_align_restZ_to_dir(pbone, dir_arm)

    rand_axis = dir_arm.cross(Vector((0,0,1)))
    if rand_axis.length < 1e-6:  
        rand_axis = Vector((1,0,0))  
    rand_axis.normalize()
    rand_angle = random.uniform(-random_angle, random_angle)
    q_rand = Quaternion(rand_axis, rand_angle)

    q_final = q_rand @ q_align

    pbone.location = explosion_loc
    pbone.rotation_quaternion = q_final
    pbone.keyframe_insert("location", frame=explosion_frame)
    pbone.keyframe_insert("rotation_quaternion", frame=explosion_frame)

    # === Frame caída
    fall_world = Vector((pos_world.x, pos_world.y, min_z_world))
    fall_arm = armature.matrix_world.inverted() @ fall_world
    
    fall_loc = explosion_loc.copy()
    fall_loc.z = fall_arm.z - pbone.bone.head_local.z  

    fall_axis = Vector((random.random(), random.random(), random.random())).normalized()
    fall_angle = random.uniform(-fall_angle_max, fall_angle_max)
    q_fall_extra = Quaternion(fall_axis, fall_angle)

    q_fall = q_fall_extra @ q_final

    pbone.location = fall_loc
    pbone.rotation_quaternion = q_fall
    pbone.keyframe_insert("location", frame=fall_frame)
    pbone.keyframe_insert("rotation_quaternion", frame=fall_frame)

    # === Rebotes
    bounce_loc = fall_loc.copy()
    bounce_rot = q_fall
    for b in range(1, num_bounces+1):
        bounce_frame = fall_frame + b * bounce_spacing
        height = bounce_height * (bounce_decay ** (b-1))

        # subir
        bounce_loc.z = fall_loc.z + height
        bounce_axis = Vector((random.random(), random.random(), random.random())).normalized()
        bounce_angle = random.uniform(-0.5, 0.5) * (1/b)  # cada vez menos rotación
        bounce_rot = Quaternion(bounce_axis, bounce_angle) @ bounce_rot

        pbone.location = bounce_loc
        pbone.rotation_quaternion = bounce_rot
        pbone.keyframe_insert("location", frame=bounce_frame)
        pbone.keyframe_insert("rotation_quaternion", frame=bounce_frame)

        # bajar
        bounce_frame += int(bounce_spacing/2)
        bounce_loc.z = fall_loc.z
        pbone.location = bounce_loc
        pbone.keyframe_insert("location", frame=bounce_frame)

print("✅ 'explosion_outward_random_fall_bounce' lista con rebotes pequeños al final.")
