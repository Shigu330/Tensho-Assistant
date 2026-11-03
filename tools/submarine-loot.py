# Builds src/TenshoAssistant/FreeCompany/Data/submarine-loot.json from the community export Submarines.json of
# FFXIVGachaSpreadsheet (https://github.com/Infiziert90/FFXIVGachaSpreadsheet, MIT, (c) 2023 Infi).
# Keeps, per sector, the number of records and for each item the total quantity seen (all surveillance tiers together).
# 0.335 (DEC-023, point 2): also keeps each loot pool (tier 1 to 3) as [records, low, mid, high, [[item, total], ...]], where low / mid /
# high are the pool's records made with a surveillance below tier 2, between tiers 2 and 3, and at tier 3 (Stats Min / Mid / High).
# Usage: python -I tools/submarine-loot.py <Submarines.json> <output.json>
import json, sys, datetime

source, output = sys.argv[1], sys.argv[2]
data = json.load(open(source, encoding="utf-8"))
sectors = []
for key, sector in sorted(data["Sectors"].items(), key=lambda pair: int(pair[0])):
    totals = {}
    for pool in sector.get("Pools", {}).values():
        for reward in pool.get("Rewards", {}).values():
            totals[reward["Id"]] = totals.get(reward["Id"], 0) + int(reward["Total"])
    if sector.get("Records", 0) <= 0 or not totals:
        continue
    tiers = []
    for name in ("Tier1", "Tier2", "Tier3"):
        pool = sector.get("Pools", {}).get(name)
        if pool is None:
            tiers.append([0, 0, 0, 0, []])
            continue
        stats = pool.get("Stats", {})
        bands = [int(stats.get("Min", 0)), int(stats.get("Mid", 0)), int(stats.get("High", 0))]
        # The three bands split the pool's records; otherwise they are not what this script expects.
        assert sum(bands) == int(pool["Records"]), (sector["Id"], name, bands, pool["Records"])
        items = {}
        for reward in pool.get("Rewards", {}).values():
            items[reward["Id"]] = items.get(reward["Id"], 0) + int(reward["Total"])
        tiers.append([int(pool["Records"])] + bands + [[[item, total] for item, total in sorted(items.items(), key=lambda pair: -pair[1])]])
    sectors.append({"Id": int(sector["Id"]), "Records": int(sector["Records"]),
                    "Items": [[item, total] for item, total in sorted(totals.items(), key=lambda pair: -pair[1])],
                    "Tiers": tiers})
result = {
    "Source": "FFXIVGachaSpreadsheet website/static/data/Submarines.json (crowd-sourced, MIT License, Copyright (c) 2023 Infi)",
    "SourceUrl": "https://github.com/Infiziert90/FFXIVGachaSpreadsheet",
    # Full MIT notice of the source (required with a substantial copy; 0.320, R-13).
    "License": "MIT License\n\nCopyright (c) 2023 Infi <infiziert@protonmail.ch>\n\nPermission is hereby granted, free of charge, to any person obtaining a copy\nof this software and associated documentation files (the \"Software\"), to deal\nin the Software without restriction, including without limitation the rights\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\ncopies of the Software, and to permit persons to whom the Software is\nfurnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all\ncopies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\nSOFTWARE.",
    "Built": datetime.date.today().isoformat(),
    "ProcessedId": data.get("ProcessedId"),
    "Sectors": sectors,
}
with open(output, "w", encoding="utf-8", newline="\n") as file:
    json.dump(result, file, ensure_ascii=False, separators=(",", ":"))
print(len(sectors), "sectors")
