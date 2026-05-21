# cvd-colored-passthrough

For usage & installation refer to [Quickstart-Guide](quickstart.md#installation).

## Introduction

Born out of a course at OVGU was the concept of trying to utilize VR/AR for visualizing *perceptual change*. 

As: 

> Virtual reality allows us to take on different perspectives and creates empathy for diverse people (age, size,
disability, etc.). Create a suitable environment that dynamically adapts to your avatar's settings, such as height,
color blindness, tunnel vision, poor hearing, sluggishness, etc.

In response to the aforementioned goal of the project my group and I developed a prototype focussing primarily on visual deficiency. The environment is able to dynamically adapt to multiple models proposed over the years to try and simulate a color vision deficient experience.

## Available models

Models are implemented not in a complete reimplementation of a given algorithm, provided one exists, but as a set of precomputed matrices provided directly by researcher or reverse-engineering. This approach was chosen to provide a simple, but yet to be scientifically evaluated, prove of concept implementation in VR.

Currently there a two models supported by the project:

1. Machado [^1]
2. Coblis v1 [^2]

For more detail on the functionally & implementation visit [Implementation](implementation.md).

Matrixes can be found at:

- [Machado](https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html) **Only accessible through university network**
- [Colvis v1](https://gist.github.com/Lokno/df7c3bfdc9ad32558bb7)

Additional models for future reference will be mentioned in [Color Vision Deficiency](cvd.md#models).

## About the models

While Machado et al.[^1] follows an actual scientific approach, simulating the perceptual changes experienced by a color vision deficient person should their respective cones malfunction, Coblis v1 is purely based on speculative experimentation. The tools used have since been taken down by the author as they wanted to stop their flawed method from propagating the internet. [^3]

> You're right, the ColorMatrix version is very simplified, and not accurate. I created that color matrix one night (http://www.colorjack.com/labs/colormatrix/)
and since then it's shown up many places... I should probably take that page down before it spreads more! Anyways, it gives you an idea of what it might look
like, but for the real thing...
>
>
> There are a few other methods, and no one really knows exactly what it would look like... these are all generalizations of a small sample, set against the masses.

Model concept will be covered in the [Models](cvd.md#models)-section wherever applicable.


[^1]: L. A. F. Fernandes, M. M. Oliveira and G. M. Machado, "A Physiologically-based Model for Simulation of Color Vision Deficiency" in IEEE Transactions on Visualization & Computer Graphics, vol. 15, no. 06, pp. 1291-1298, November/December 2009, doi: 10.1109/TVCG.2009.113.
keywords: {Models of Color Vision;Color Perception;Simulation of Color Vision Deficiency;Anomalous Trichromacy;Dichromacy.}
URL: https://doi.ieeecomputersociety.org/10.1109/TVCG.2009.113




[^2]: https://gist.github.com/Lokno/df7c3bfdc9ad32558bb7

[^3]: https://github.com/MaPePeR/jsColorblindSimulator/blob/master/README.md

