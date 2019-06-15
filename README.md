# Ryzen Instruction Monitor

Lazy ass example for reading Ryzen Performance Monitor Counters (PMC) as defined in the [Open-Source Register Reference For AMD Family 17h ProcessorsModels 00h-2Fh](https://developer.amd.com/wp-content/resources/56255_3_03.PDF).

![Screenshot](https://i.imgur.com/XA4prI4.png)

Currently reading the following counters:

- IS: PMCx0C1
- BI: PMCx0C2
- LS: PMCx029
- FP: PMCx0C0
- FM: PMCx003

Displayed as "GI", meaning "Giga Instructions per Second".

Using [WinRing0](https://github.com/QCute/WinRing0).
