# X-158 — Highbridge (Le Viaduc, vista 020): window and weather

Read on 2026-11-12 from the installed game's sheets (patch 7.58) with `tools/XbmProbe` (raw columns), and from the wiki's Sightseeing Log
(community data, the only source of the weather a vista needs; the game data does not hold it).

## Adventure (game data)

Row 2162707, « Le Viaduc » (French client), MinTime 500, MaxTime 759: the window is 05:00 to 07:59 Eorzea time, MaxTime included.

```
2162707|4877082|0|0|58|1|2|500|759|44|81556|81056|81453|True|Le Viaduc|…
```

## Weather of the zone (game data)

TerritoryType 145 (`w1f3`, Eastern Thanalan) uses WeatherRate 11:

```
11|1|40|2|20|3|10|4|10|7|5|8|15|0|0|0|0
```

Clear Skies (1) 40, Fair Skies (2) 20, Clouds (3) 10, Fog (4) 10, Rain (7) 5, Showers (8) 15.

## Wiki row (community data)

ARR 20, Highbridge, Eastern Thanalan, weather « Fair Skies or Clear Skies », time 05:00-08:00, emote Pray.

## The user's report (12/11, local time UTC+1)

- 00:32 (23:32 UTC on 11/11): refused by the game. Eorzea time 14:24 (outside 05:00–07:59); weather Clear Skies (period from 23:13:20 UTC).
- 01:14 (00:14 UTC): accepted. The window opens at 05:00 ET, 00:14:35 UTC; weather Fair Skies (period from 00:00 UTC to 00:23:20 UTC, which
  is also when the window closes at 08:00 ET).

`VistaWindow` (src/TenshoAssistant/Maps/VistaWindow.cs) gives exactly this; test VistaWindow1128Tests.
