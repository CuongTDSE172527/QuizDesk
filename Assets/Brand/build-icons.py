"""Render the original QuizDesk vector mark and platform icon containers.
Requires Pillow. Run from any directory; no network or external artwork.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import json

root=Path(__file__).resolve().parents[2]
brand=Path(__file__).resolve().parent
svg='''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128">
<title>QuizDesk Q cards and check</title>
<rect width="128" height="128" rx="28" fill="#4659B7"/>
<rect x="40" y="24" width="62" height="60" rx="14" fill="none" stroke="#B8C7FF" stroke-width="6"/>
<rect x="25" y="34" width="62" height="60" rx="14" fill="#4659B7" stroke="#FFFFFF" stroke-width="7"/>
<path d="M42 61 L53 72 L73 50" fill="none" stroke="#FFFFFF" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"/>
<path d="M73 79 L100 102" fill="none" stroke="#62D8C2" stroke-width="8" stroke-linecap="round"/>
</svg>'''
(brand/'QuizDesk.svg').write_text(svg,encoding='utf-8')
scale=8
mark=Image.new('RGBA',(128*scale,128*scale));d=ImageDraw.Draw(mark)
def box(values):return tuple(int(x*scale) for x in values)
def rounded(values,radius,fill=None,outline=None,width=1):
    d.rounded_rectangle(box(values),radius*scale,fill=fill,outline=outline,width=width*scale)
def stroke(points,color,width):
    d.line([(x*scale,y*scale) for x,y in points],fill=color,width=width*scale,joint='curve')
    radius=width/2
    for x,y in points:d.ellipse(box((x-radius,y-radius,x+radius,y+radius)),fill=color)
rounded((0,0,128,128),28,fill='#4659B7')
# Expand outline geometry outward to match SVG's centered strokes.
rounded((37,21,105,87),17,outline='#B8C7FF',width=6)
rounded((21.5,30.5,90.5,97.5),17.5,fill='#4659B7',outline='#FFFFFF',width=7)
stroke([(42,61),(53,72),(73,50)],'#FFFFFF',8)
stroke([(73,79),(100,102)],'#62D8C2',8)
def image(size,opaque=False):
    result=mark.resize((size,size),Image.Resampling.LANCZOS)
    if opaque:
        bg=Image.new('RGBA',result.size,'#4659B7');bg.alpha_composite(result);return bg.convert('RGB')
    return result
image(512).save(brand/'QuizDesk.png')
image(256).save(root/'QuizDesk.Mobile/Assets/QuizDesk-logo.png')
image(1024).save(root/'Assets/QuizDesk.icns',format='ICNS')
image(1024).save(root/'QuizDesk.Desktop/QuizDesk.ico',format='ICO',sizes=[(n,n) for n in [16,20,24,32,40,48,64,128,256]])
image(512).save(root/'QuizDesk.Android/Resources/drawable/icon.png')
iconset=root/'QuizDesk.iOS/Assets.xcassets/AppIcon.appiconset'
for item in json.loads((iconset/'Contents.json').read_text())['images']:
    size=int(round(float(item['size'].split('x')[0])*float(item['scale'].rstrip('x'))))
    image(size,True).save(iconset/item['filename'])
print('Rendered SVG master, shared UI logo, ICO, ICNS, Android PNG and opaque iOS icon sizes.')
resources=root/'QuizDesk.Android/Resources'
vector='''<vector xmlns:android="http://schemas.android.com/apk/res/android" android:width="108dp" android:height="108dp" android:viewportWidth="128" android:viewportHeight="128">
<group android:scaleX="0.9" android:scaleY="0.9" android:pivotX="64" android:pivotY="64">
<path android:pathData="M54,24 H88 Q102,24 102,38 V70 Q102,84 88,84 H54 Q40,84 40,70 V38 Q40,24 54,24 Z" android:fillColor="#00000000" android:strokeColor="#B8C7FF" android:strokeWidth="6"/>
<path android:pathData="M39,34 H73 Q87,34 87,48 V80 Q87,94 73,94 H39 Q25,94 25,80 V48 Q25,34 39,34 Z" android:fillColor="#4659B7" android:strokeColor="#FFFFFF" android:strokeWidth="7"/>
<path android:pathData="M42,61 L53,72 L73,50" android:fillColor="#00000000" android:strokeColor="#FFFFFF" android:strokeWidth="8" android:strokeLineCap="round" android:strokeLineJoin="round"/>
<path android:pathData="M73,79 L100,102" android:fillColor="#00000000" android:strokeColor="#62D8C2" android:strokeWidth="8" android:strokeLineCap="round"/>
</group></vector>'''
(resources/'drawable/ic_launcher_foreground.xml').write_text(vector)
# Monochrome uses transparent card interiors so launchers can supply their themed background.
mono=vector.replace('#B8C7FF','#FFFFFF').replace('#62D8C2','#FFFFFF').replace('android:fillColor="#4659B7"','android:fillColor="#00000000"')
(resources/'drawable/ic_launcher_monochrome.xml').write_text(mono)
for qualifier in ['mipmap-anydpi-v26','mipmap-anydpi-v33']:
    folder=resources/qualifier;folder.mkdir(exist_ok=True)
    themed='<monochrome android:drawable="@drawable/ic_launcher_monochrome"/>' if qualifier.endswith('33') else ''
    (folder/'ic_launcher.xml').write_text('<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android"><background android:drawable="@color/quizdesk_launcher"/><foreground android:drawable="@drawable/ic_launcher_foreground"/>'+themed+'</adaptive-icon>')
(resources/'values/launcher.xml').write_text('<resources><color name="quizdesk_launcher">#4659B7</color></resources>')
