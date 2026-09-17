import bpy
bpy.ops.wm.open_mainfile(filepath='C:/Program Files (x86)/Steam/steamapps/common/Valheim/Magenheim/assets/models/source/architecture-crystal-beam-2m.blend')
for im in bpy.data.images:print('IMAGE',im.name,im.type,im.source,list(im.size),im.has_data,im.packed_file)
