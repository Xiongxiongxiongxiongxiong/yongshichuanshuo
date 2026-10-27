from PIL import Image
import numpy as np
from pathlib import Path
import json
for path in sorted(Path('Assets/player/SwordCollection').glob('*/Supplement.png')):
    a=np.asarray(Image.open(path).convert('RGBA'))[:,:,3]
    h,w=a.shape
    hist=(a[:,:int(w/4)]>200).sum(1)
    smooth=np.convolve(hist,np.ones(9)/9,mode='same')
    # Find the low-density gutter between each consecutive authored action row.
    cuts=[0]
    for n in range(1,9):
        target=n*h/9
        lo=int(target-h/9*.42);hi=int(target+h/9*.35)
        best=min(range(max(cuts[-1]+40,lo),min(h,hi)),key=lambda y:smooth[y]+.07*abs(y-target))
        cuts.append(best)
    cuts.append(h)
    if path.parent.name=='iron_longsword': cuts=[0,148,296,396,536,684,812,957,1132,h]
    (path.parent/'Slices.json').write_text(json.dumps({'supplementRows':cuts},indent=2))
    print(path.parent.name,h,w,cuts)
