"""Print JSON advance widths in em for a font and a JSON array of strings.

Used by animated-svg-utils.mjs through uv with fonttools[woff]. Kerning is
deliberately excluded to match the SVG renderers' text measurements.
"""

import json
import sys

from fontTools.ttLib import TTFont


with TTFont(sys.argv[1]) as font:
    character_map = font.getBestCmap()
    metrics = font["hmtx"]
    units_per_em = font["head"].unitsPerEm
    texts = json.loads(sys.argv[2])
    widths = [
        sum(metrics[character_map[ord(character)]][0] for character in text)
        / units_per_em
        for text in texts
    ]

print(json.dumps(widths))
