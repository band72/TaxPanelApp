import json
from PIL import Image, ImageDraw, ImageFont

img_path = r"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\map.png"
img = Image.open(img_path)

left = 135
top = 135
right = 80
bottom = 130

gridWidth = 1024 - left - right
gridHeight = 1024 - top - bottom

secW = gridWidth / 42.0
secH = gridHeight / 36.0

draw = ImageDraw.Draw(img)

test_cases = [
    ("6307", 6, 3, 7),
    ("6418", 6, 4, 18),
    ("5319", 5, 3, 19),
]

for name, rDigit, tDigit, sec in test_cases:
    tIndex = tDigit - 1
    rIndex = rDigit - 3
    
    sectionZeroBased = sec - 1
    rowInTownship = sectionZeroBased // 6
    colInTownship = sectionZeroBased % 6
    if rowInTownship % 2 == 0:
        colInTownship = 5 - colInTownship
        
    gridCol = rIndex * 6 + colInTownship
    gridRow = tIndex * 6 + rowInTownship
    
    x = left + gridCol * secW
    y = top + gridRow * secH

    draw.rectangle([x, y, x + secW, y + secH], outline="blue", width=2)
    
    crop = img.crop((int(x)-30, int(y)-30, int(x+secW)+30, int(y+secH)+30))
    crop.save(rf"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\fixed_crop_{name}.png")

img.save(r"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\test_map_fixed.png")
print("Saved fixed crops.")
