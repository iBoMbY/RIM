# Ryzen Instruction Monitor

Lazy ass example for reading Ryzen Performance Monitor Counters (PMC) as defined in the [Open-Source Register Reference For AMD Family 17h ProcessorsModels 00h-2Fh](https://developer.amd.com/wp-content/resources/56255_3_03.PDF).

![Screenshot](https://i.imgur.com/XA4prI4.png)

Currently reading the following counters:

- IS: PMCx0C1 [Retired Uops]
- BI: PMCx0C2 [Retired Branch Instructions]
- LS: PMCx029 [LS Dispatch]
- FP: PMCx0C0 [Retired Instructions] <- ? This is probably the wrong one?
- FM: PMCx003 [Retired SSE/AVX Operations]

Displayed as "GI", meaning "Giga Instructions per Second".

Using [WinRing0](https://github.com/QCute/WinRing0).
