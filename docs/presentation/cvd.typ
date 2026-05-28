#import "@preview/parcio-slides:0.1.2": *
#show: parcio-theme

#title-slide(
  title: "Color Vision Deficiency",
  subtitle: "Simulation via VR passthrough",
  logo: image("../static/ovgu.svg", width: 9.8cm),
  extra: [
    #set text(0.825em)
    Faculty of Computer Science\
    Otto von Guericke University Magdeburg
  ],
  author: (name: "Lucas Philipp", mail: "lucas.philipp@st.ovgu.de"),
  date: "29.05.2026",
)

// Show presentation title in outline and highlight upcoming section.
#outline-slide(show-title: true, new-section: "Introduction")

/* ---------- */

#slide(title: "CVD", new-section: "Introduction")[
  #align(
    [#image("../static/cvd.jpg", height: 10.1cm) @example],
    center,
  )
]

#slide(
  title: "Affected population",
)[
  #figure(
    caption: [Maps showing global prevalence of congenital color vision deficiency among children and adolescents, 1980 through 2022 @population],
  )[ #image("../static/population.png")]
]

#slide(
  title: "Affected population",
)[
  #show table.cell.where(y: 0): strong
  #set table(
    stroke: (x, y) => if y == 0 {
      (bottom: 0.7pt + black)
    },
    align: (x, y) => (
      if x > 0 { center } else { left }
    ),
  )

  #align(
    [#table(
        columns: 3,
        column-gutter: 10pt,
        table.header([Type], [Affected Cones], [ Prevalence ]),
        table.hline(start: 0),
        [ Protanopia], [L (red) absent], [ ~1% males ],
        [ Deuteranopia], [M (green) absent], [ ~6% males ],
        [ Tritanopia], [ S (blue) absent], [ ~0.67% males ],
        [ Achromatopsia], [ all three absent ], [ NA ],
        [ Anomalous trichromacy], [ any impacted], [ males 1.17% ],
        [ Dichromacy], [ two present], [ males 1.59% ],
        [ Monochromacy], [ one present], [ males 0.36% ],
      ) @population @numbers],
    center,
  )
]

#slide(
  title: "How does CVD happen?",
)[
  #figure(
    caption: [LMS color matching functions from CIE, based on the Stiles and Burch 10° color matching functions. @lms],
  )[ #image("../static/lms.webp")]
]

#slide(
  title: "Lines of confusion",
)[
  #image("../static/lines.png") @lines
]

#slide(title: "Implementation", new-section: "Project")[
  #align(
    [#image("../static/start screne.png")],
    center,
  )
]

#slide(title: "Implementation")[
  #align(
    [#grid(
      columns: 2,
      image("../static/ingame.png"), image("../static/ingame2.png"),
    )],
    center,
  )
]

#slide(title: "Implementation")[
  Demo or Try it yourself
]

#bib-slide(bibliography(
  "bibliography/report.bib",
  title: none,
  style: "bibliography/apalike.csl",
))
