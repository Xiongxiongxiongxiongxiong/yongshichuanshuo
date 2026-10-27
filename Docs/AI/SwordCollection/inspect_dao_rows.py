from PIL import Image
import numpy as np
from pathlib import Path
import json

swords=['iron_longsword','iron_greatsword','jade_longsword','verdant_greatsword','frost_longsword','tide_greatsword','amethyst_longsword','thunder_greatsword','dragon_longsword','solar_greatsword']
daos=['iron_dao','iron_cleaver','jade_dao','verdant_cleaver','frost_dao','tide_cleaver','amethyst_dao','thunder_cleaver','dragon_dao','phoenix_cleaver']
for sword,dao in zip(swords,daos):
    path=Path('Assets/player/DaoCollection')/dao/'Supplement.png'
    if not path.exists(): continue
    a=np.asarray(Image.open(path).convert('RGBA'))[:,:,3];h,w=a.shape
    hist=(a[:,:w//4]>200).sum(1)
    smooth=np.convolve(hist,np.ones(7)/7,mode='same')
    template=Path('Assets/player/SwordCollection')/sword
    oldh=Image.open(template/'Supplement.png').height
    prior=json.loads((template/'Slices.json').read_text())['supplementRows']
    cuts=[0]
    for value in prior[1:-1]:
        target=value*h/oldh;radius=h/9*.16
        lo=max(cuts[-1]+40,int(target-radius));hi=min(h,int(target+radius))
        cuts.append(min(range(lo,hi),key=lambda y:smooth[y]+.15*abs(y-target)))
    cuts.append(h)
    (path.parent/'Slices.json').write_text(json.dumps({'supplementRows':cuts},indent=2))
    print(dao,cuts)
