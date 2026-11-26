# Cinema Ticket Booking System – One-Pager
Domäne: Kinobetrieb (Programm, Vorstellungen, Platzbuchung) · Architektur: Clean Architecture (Domain,
Services, Repository-Ports, Adapter) · Plattform: C# 14 / .NET 10 LTS · Währung: EUR

## 01 Zielsetzung
Fachliches Ziel. Das System ermöglicht Kunden, das aktuelle Programm einzusehen, freie Plätze zu prüfen,
Tickets zu buchen und eigene Buchungen zu stornieren. Kinomanager pflegen den Filmkatalog und planen
Vorstellungen ein oder sagen sie ab. Das System verhindert inkonsistente Buchungen, erzwingt die
Saalbelegungsregeln und hält die Platzintegrität aufrecht. Zahlung und Authentifizierung liegen bewusst
außerhalb des Systems.

### Technisches Ziel
Ein wartbarer, testbarer und von der Infrastruktur unabhängiger Kern, der SOLID- und
Clean-Code-Prinzipien folgt und durchgängig zwischen Anforderung, Modell, Code und Test rückverfolgbar ist.

## 02 Erfolgskriterien

### Keine Doppelbuchung
Ein Sitzplatz kann pro Vorstellung nur in einer bestätigten Buchung vorkommen; Verfügbarkeit
ergibt sich ausschließlich aus CONFIRMED -Buchungen.
### Strikte Zukunftsregel
Einplanen und Absagen von Vorstellungen sowie Buchen und Stornieren von Buchungen sind
nur möglich, solange der Vorstellungsbeginn echt in der Zukunft liegt.
### Saalbelegung
Keine Vorstellung in einem Saal, der durch eine andere SCHEDULED -Vorstellung belegt ist
(Überschneidungsprüfung inklusive 30 Minuten Puffer).
### Unveränderlichkeit
Domänenobjekte sind Records; Zustandswechsel erzeugen neue Instanzen ( Cancel() ).
### Geld in EUR
Das Value Object Money erzwingt positive Beträge, normalisiert sie auf Scale 2 und lehnt Werte
mit mehr als zwei Nachkommastellen ab.
### Determinismus
Zeitabhängigkeit über einen injizierbaren System.TimeProvider in ShowtimeService und
BookingService ; Tests mit xUnit, FluentAssertions und programmatischem FakeItEasy (ohne
Attribute).
### Abgrenzung (out of scope)
Zahlungsabwicklung, Kundenauthentifizierung, Saal-Stammdatenpflege, produktive Datenbank und nebenläufigkeitsfähige Adapter.

## 03 Prozessbeschreibung

### Zustandsmodelle
Vorstellung: [*] ─ planen ─► SCHEDULED ─ absagen ─► CANCELLED ─► [*]
Buchung: [*] ─ buchen ─► CONFIRMED ─ stornieren / Vorstellung abgesagt ─► CANCELLED ─► [*]

### Use Cases im Überblick
| UC | Prozess | Akteur | Kernlogik | Ergebnis |
| ---- | ---- | ---- | ---- | ---- |
| 01 | Film anlegen | Manager | Titel und Genre nicht leer, Dauer positiv; das System vergibt eine eindeutige ID (GUID) | Film im Katalog |
| 02 | Vorstellung einplanen | Manager | Film und Saal müssen existieren; Beginn echt in der Zukunft; Preis positiv in EUR; keine Überschneidung mit SCHEDULED-Vorstellungen desselben Saals | Vorstellung SCHEDULED |
| 03 | Vorstellung absagen | Manager | nur SCHEDULED und Beginn in der Zukunft; alle bestätigten Buchungen werden storniert, Plätze frei | Vorstellung CANCELLED , Audit bleibt erhalten |
| 04 | Programm durchsuchen | Kunde | nur SCHEDULED und echt zukünftige Vorstellungen, nach Film gruppiert, nach Beginn sortiert | Programmübersicht |
| 05 | Plätze wählen und buchen | Kunde | freie Plätze = Saalraster abzüglich bestätigter Buchungen; mindestens ein Platz, alle im Saalraster und frei; Gesamtpreis = Basispreis × Anzahl Plätze; logisch atomar | Buchung CONFIRMED |
| 06 | Buchung stornieren | Kunde | Besitzer, Status CONFIRMED , Vorstellung SCHEDULED und Beginn echt in der Zukunft | Buchung CANCELLED , Plätze frei |
| 07 | Buchungen einsehen | Kunde | alle eigenen Buchungen, bestätigte und stornierte, nach Vorstellungsbeginn sortiert | Buchungsliste |

### Zentrale Regeln
Saalbelegung: neuerBeginn < bestehendesEnde UND neuesEnde > bestehenderBeginn
Endzeit: Beginn + Filmdauer + 30 Minuten Puffer
Gesamtpreis: Basispreis × Anzahl Plätze (EUR, Scale 2)

### Ablauf und Fehlerverhalten
Die Services folgen einem einheitlichen Muster: Eingaben validieren → Objekte über Ports laden →
Geschäftsregeln und Zustand prüfen → neue unveränderliche Instanz erzeugen → speichern → Ergebnis
zurückgeben. Die Buchung ist logisch atomar: Ist ein Platz ungültig oder vergeben oder die Vorstellung nicht
buchbar, wird nichts gespeichert. Fehler werden über ArgumentException (ungültige Eingabe, unbekannte ID),
InvalidOperationException (unzulässiger Zustand, belegter Saal oder Platz) und
UnauthorizedAccessException (fremde Buchung) signalisiert. Stornierte Vorstellungen und Buchungen
bleiben zur Nachvollziehbarkeit gespeichert.