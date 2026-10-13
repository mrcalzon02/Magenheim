from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-wrap-r6').replace('creature-models/2026-10-04','creature-models/2026-10-13').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
source=source.replace("('side',(2,0,.06))","('side',(-2,0,.06))")
source=source.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))", " if view=='side':assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0, 'side camera must project front +Y to screen-left'\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
exec(compile(source,str(__file__),'exec'))
(OUT/'side-orientation.json').write_text(json.dumps(dict(source=row['source'],source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),camera_direction=[-2,0,.06],model_front_axis='+Y',screen_direction='front/head left, back right',method='saved model from -X; projected +Y guard passed'),indent=2)+'\n',encoding='utf-8')
