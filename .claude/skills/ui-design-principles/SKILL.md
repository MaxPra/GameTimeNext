---
name: "ui-design-principles"
description: "Beim Entwerfen oder Überarbeiten von Benutzeroberflächen (WPF, Web, Mockups) anwenden: HIG-Grundprinzipien ohne Apple-Optik."
---

# UI-Designprinzipien (angelehnt an Apple HIG, ohne Apple-Optik)

Übernimm die Denkweise der Apple Human Interface Guidelines, aber NICHT deren Aussehen. Die Oberfläche darf Ideen aufgreifen, aber kein Klon eines Apple-Produkts werden.

## Übernehmen (Prinzipien)

- **Hierarchie:** Das Wichtigste ist auf einen Blick erkennbar. Primäre Aktion klar hervorheben, Nebensächliches zurücknehmen.
- **Klarheit:** Gut lesbare Schriftgrößen, eindeutige Beschriftungen, keine mehrdeutigen Icons ohne Text. Es darf nichts abgeschnitten sein: Beschriftungen stehen in Spalten mit automatischer Breite, nicht in festen Breiten, die nur mit der gerade verwendeten Schrift und dem gerade verwendeten Text passen.
- **Zurückhaltung:** Die Oberfläche dient dem Inhalt. Keine Effekte, Verläufe oder Animationen ohne Zweck.
- **Konsistenz:** Gleiche Abstände, gleiche Bedienelemente und gleiches Verhalten im ganzen Programm. Ein festes Abstandsraster (z. B. 4 oder 8 px) verwenden.
- **Einheitliche Höhe:** Buttons, Textboxen, ComboBoxen und andere Eingabeelemente sind gleich hoch, damit sie nebeneinander eine saubere Linie bilden. Die Höhe kommt aus einem gemeinsamen Wert (in GTN: `Control.Height`), nicht aus Padding und Textgröße, und einzelne Views setzen keine eigene Höhe. Nach Style-Änderungen die tatsächliche Höhe messen: Nur Listenzeilen dürfen kleinere Elemente haben.
- **Vertraute Muster:** Bekannte Bedienmuster der Zielplattform nutzen (unter Windows: Windows-Konventionen, nicht Apple-Konventionen).
- **Feedback und Kontrolle:** Jede Aktion bekommt eine sichtbare Rückmeldung (Ladezustand, Erfolg, Fehler). Destruktive Aktionen bestätigen lassen oder rückgängig machbar machen.
- **Barrierefreiheit:** Ausreichender Kontrast, vollständige Tastaturbedienung, sichtbarer Fokus, Bedeutung nie nur über Farbe vermitteln.

## Nicht übernehmen (Apple-Optik)

- Keine SF-Pro-Schriften und keine SF Symbols
- Keine Apple-Systemfarben (z. B. das typische Apple-Blau als Akzent)
- Glas-Effekte sind erlaubt, aber nur in der eigenen, dezenten Ausprägung des Projekts (in GTN: durchscheinende Flächen mit feiner Lichtkante), nie als 1:1-Kopie von Apple-Materialien
- Keine Apple-typischen Navigationsmuster wie Tab Bars am unteren Rand
- Schalter sind erlaubt, aber in der eigenen Optik des Projekts
- Keine stark gerundeten, Apple-typischen Karten und Buttons als Standard

## Eigene Optik

1. Wenn das Projekt bereits Farben, Schriften, Ressourcen-Dictionaries (WPF) oder CSS-Variablen hat: diese verwenden.
2. Wenn es keine gibt: eine eigenständige, neutrale Optik wählen und die gewählten Werte (Schrift, Akzentfarbe, Eckenradius, Abstandsraster) zu Beginn kurz nennen, damit sie angepasst werden können.