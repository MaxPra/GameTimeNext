---
name: "ui-design-principles"
description: "Beim Entwerfen oder Überarbeiten von Benutzeroberflächen (WPF, Web, Mockups) anwenden: HIG-Grundprinzipien ohne Apple-Optik."
---

# UI-Designprinzipien (angelehnt an Apple HIG, ohne Apple-Optik)

Übernimm die Denkweise der Apple Human Interface Guidelines, aber NICHT deren Aussehen. Die Oberfläche soll nicht nach iOS oder macOS aussehen.

## Übernehmen (Prinzipien)

- **Hierarchie:** Das Wichtigste ist auf einen Blick erkennbar. Primäre Aktion klar hervorheben, Nebensächliches zurücknehmen.
- **Klarheit:** Gut lesbare Schriftgrößen, eindeutige Beschriftungen, keine mehrdeutigen Icons ohne Text.
- **Zurückhaltung:** Die Oberfläche dient dem Inhalt. Keine Effekte, Verläufe oder Animationen ohne Zweck.
- **Konsistenz:** Gleiche Abstände, gleiche Bedienelemente und gleiches Verhalten im ganzen Programm. Ein festes Abstandsraster (z. B. 4 oder 8 px) verwenden.
- **Vertraute Muster:** Bekannte Bedienmuster der Zielplattform nutzen (unter Windows: Windows-Konventionen, nicht Apple-Konventionen).
- **Feedback und Kontrolle:** Jede Aktion bekommt eine sichtbare Rückmeldung (Ladezustand, Erfolg, Fehler). Destruktive Aktionen bestätigen lassen oder rückgängig machbar machen.
- **Barrierefreiheit:** Ausreichender Kontrast, vollständige Tastaturbedienung, sichtbarer Fokus, Bedeutung nie nur über Farbe vermitteln.

## Nicht übernehmen (Apple-Optik)

- Keine SF-Pro-Schriften und keine SF Symbols
- Keine iOS-Systemfarben (z. B. das typische iOS-Blau als Akzent)
- Keine Glas-, Blur- oder Milchglas-Effekte
- Keine iOS-Navigationsmuster wie Tab Bars am unteren Rand oder iOS-Schalter-Optik
- Keine stark gerundeten iOS-typischen Karten und Buttons als Standard

## Eigene Optik

1. Wenn das Projekt bereits Farben, Schriften, Ressourcen-Dictionaries (WPF) oder CSS-Variablen hat: diese verwenden.
2. Wenn es keine gibt: eine eigenständige, neutrale Optik wählen und die gewählten Werte (Schrift, Akzentfarbe, Eckenradius, Abstandsraster) zu Beginn kurz nennen, damit sie angepasst werden können.