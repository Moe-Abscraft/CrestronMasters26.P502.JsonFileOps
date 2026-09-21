# Instructor Guide — JSON Objects & File Operations
### Programming in C# for Beginners · Crestron Masters

**Audience:** Crestron programmers new to C#. They think in signals, symbols, and modules — not classes and streams. Your job is to bridge that gap.

**Running example (use it the whole class, never switch):** a **room configuration** — room name, display count, auto-shutdown, a list of displays, a list of presets. It carries every concept from "what is JSON" through robust file I/O.

**The one sentence that anchors everything:** *"JSON is a standard way to write down the structured data you're already dealing with, so it lives outside your code and can be read, changed, and shared."*

---

## Class flow at a glance

1. **Open with the problem, not syntax** — everyone has hard-coded a room name/IP/preset and had to recompile. JSON puts that data *outside* the code.
2. **What JSON is** — text format for structured data; readable, machine-parseable, language-independent. Keys and values, that's the idea.
3. **Why JSON vs XML vs others** — same data both ways; JSON is lighter and maps to objects/arrays; be fair to XML; one line each on YAML/CSV/INI.
4. **The building blocks** — the six types, nesting, and the gotchas (double quotes, no trailing commas, no comments, lowercase booleans). Spot-the-error exercise.
5. **Mapping JSON to C# objects** — object=class, key=property, array=List, nested object=nested class; the camelCase↔PascalCase bridge.
6. **Serialize / deserialize** — the two words; System.Text.Json; the round trip.
7. **File operations** — read→deserialize→use; change→serialize→write back; processor paths; first-run default.
8. **The two failure modes** — power loss → temp-then-rename; thread collision → CCriticalSection.
9. **Capstone** — walk the ConfigManager; one Save() handles both failures; Load() always returns something usable.
10. **Wrap-up** — recap the chain; hand out the cheat sheet; "it built" vs "it loaded and ran on the box."

---

## What's in this kit

| File | Use it for |
|---|---|
| `RoomConfig.cs` | The core "JSON object = C# class" mapping. Show on screen next to the JSON. |
| `sample-roomconfig.json` | Real JSON students can read alongside the class. |
| `ConfigManager.cs` | The capstone — and the code you drive the live demos from. |
| `Student-Cheat-Sheet.md` | One-page reference to hand out. |

**One project, one runtime.** Everything lives in your **main SIMPL# Pro control-system project**, targeting **.NET 8** and deploying to **VC-4**. There's no separate console demo project and no serialization NuGet package — **System.Text.Json is built into .NET 8**, so there's nothing to install and no version conflict to chase.

**What that means for the live demos (plan for this).** Because there's no standalone console app that runs on any PC, the serialize/deserialize and file demos run **on a VC-4 target** — via console commands and `ErrorLog` output. So:
- Have a **VC-4 instance reachable in the room**, with the program deployed before you start.
- Register two console commands (`CrestronConsole.AddNewConsoleCommand`): **`roomconfig`** dumps the live config (shows serialization) and **`roomreload`** re-reads the file (shows deserialization).
- If you *won't* have a live VC-4, modules 5–6 become a **code walkthrough** using `sample-roomconfig.json` and the expected log output — still effective, just not interactive.

---

## Timing

Total below is ~2 hours of content plus a capstone. Scale to your slot — modules 1–6 are the core; the capstone can be a walkthrough instead of a build-along if you're short.

| # | Module | ~Min |
|---|---|---|
| 1 | What JSON is | 10 |
| 2 | Why JSON vs XML vs others | 10 |
| 3 | The building blocks + gotchas | 15 |
| 4 | Mapping JSON to C# objects | 15 |
| 5 | Serialize / deserialize | 15 |
| 6 | File operations | 15 |
| 7 | Robustness: the two failure modes | 15 |
| 8 | Capstone: the config manager | remainder |

---

## Module 1 — What JSON is (10 min)

**Goal:** demystify the word before any syntax.

Say: JSON = **J**ava**S**cript **O**bject **N**otation. A text format for structured data. Human-readable, machine-parseable, and **language-independent** — the "JavaScript" in the name trips people up, so say out loud: *it's used everywhere, not just JavaScript.*

Show the simplest possible thing and nothing more:
```json
{ "roomName": "Boardroom A", "displayCount": 2, "autoShutdown": true }
```
Point at it: *"It's just labeled values — keys and values. That's the whole idea."*

---

## Module 2 — Why JSON vs XML vs others (10 min)

**Goal:** justify the choice; they've seen XML in configs.

Show the **same data** both ways. JSON (above) vs XML:
```xml
<room>
  <roomName>Boardroom A</roomName>
  <displayCount>2</displayCount>
</room>
```
Make it concrete, not abstract:
- JSON is lighter (no closing tags), reads more easily, and maps **directly** onto objects and arrays — which is why modern web APIs speak JSON. That matters because Crestron programs increasingly call REST APIs.
- Be fair to XML: schemas (XSD) for validation, namespaces, attributes, and it allows comments (strict JSON doesn't).
- One line each on the neighbors: YAML (config-friendly, whitespace-sensitive), CSV (flat tables only), INI/key-value (flat, no nesting).

**Takeaway:** JSON is today's default for configs and APIs.

---

## Module 3 — The building blocks + gotchas (15 min)

**Goal:** the six value types, then the mistakes that cause 90% of early pain.

Six types: **string** (always double quotes), **number**, **boolean** (lowercase `true`/`false`), **null**, **object** `{ }`, **array** `[ ]`.

Then nesting — open `sample-roomconfig.json` and show the `displays` array of objects and the `presets` array of strings.

**Spot-the-error exercise** — put this on screen and have them call out what's wrong (answers below):
```
{
  'roomName': 'Boardroom A',      // <- (a)
  "displayCount": 2,              // <- (b)
  "autoShutdown": true,           // <- (c) trailing comma
  // main display                 // <- (d)
}
```
Answers: (a) single quotes aren't valid JSON — must be double quotes. (b) fine. (c) trailing comma after the last item is invalid. (d) comments aren't allowed in strict JSON. This exercise sticks better than any slide.

> Tie-in for later: **System.Text.Json enforces every one of these rules by default** — it rejects comments and trailing commas unless you opt in. The parser itself becomes your syntax teacher in Module 5.

---

## Module 4 — Mapping JSON to C# objects (15 min)

**Goal:** the conceptual heart. Open `RoomConfig.cs` beside `sample-roomconfig.json`.

Drive the one-to-one mapping:
- A JSON **object** -> a C# **class**.
- A JSON **key** -> a **property**.
- A JSON **array** -> a `List<T>`.
- A **nested object** -> a **nested class** (`DisplayInfo`).

Cover the naming bridge: JSON is usually camelCase (`roomName`), C# is PascalCase (`RoomName`). The `[JsonPropertyName("roomName")]` attribute reconciles them. Note that System.Text.Json is **case-sensitive by default** — so being explicit with the attribute isn't optional politeness, it's what makes the match work. This is also a natural moment to reinforce identifier naming rules (PascalCase, start with a letter, no hyphens).

---

## Module 5 — Serialize / deserialize (15 min)

**Goal:** the aha moment.

Define both directions plainly and keep repeating the words:
- **Serialize** = object -> JSON text (writing out).
- **Deserialize** = JSON text -> object (reading in).

**Library: System.Text.Json.** Say why it's the pick here: it's **built into .NET 8** (no NuGet package, nothing to install), it's Microsoft's default for new .NET code, and because there's no extra assembly, there's **no version conflict** to debug on the box. Mention Newtonsoft.Json exists and is worth reaching for when you need its extra features (lenient parsing of hand-edited files, merging into an existing object, and more) — but a simple config doesn't need them.

The API:
```csharp
using System.Text.Json;

// SERIALIZE: object -> text
string json = JsonSerializer.Serialize(room,
    new JsonSerializerOptions { WriteIndented = true });

// DESERIALIZE: text -> object
RoomConfig room = JsonSerializer.Deserialize<RoomConfig>(json);
```

**The round trip is the best single demo of the class:** object -> string -> show it -> back to object. On VC-4: deploy the program, then run the **`roomconfig`** console command to dump the serialized config to the console, and watch the values come back through `ErrorLog`. It's the same lesson as a desktop console app, just driven from the processor.

---

## Module 6 — File operations (15 min)

**Goal:** connect to the file system — the *why* behind storing configs.

The real workflow: **read file -> deserialize -> use it**; then **change something -> serialize -> write it back.**

Two Crestron-specific flags a generic tutorial won't give them:
- File paths on a processor aren't like a Windows PC. Use Crestron's path helpers and `Crestron.SimplSharp.CrestronIO`, not hard-coded Windows paths. (Point to `ConfigManager.cs`.)
- On first run the file won't exist. Handle it by generating a default rather than throwing.

**Demo on VC-4:** run **`roomreload`** to re-read the file after you've edited it, and watch the new values land through `ErrorLog`. Edit the file, reload, see the change — that's read + deserialize made visible.

---

## Module 7 — Robustness: the two failure modes (15 min)

**Goal:** beginners assume the happy path. Teach the two failures as a **pair** — they're different problems.

**Power loss mid-write -> write-to-temp-then-rename.** `WriteAllText` truncates the file to zero, *then* writes. Lose power in that window and you're left with an empty/half-written config. Fix: write to a temp file in the same folder, then swap it over the real file. The swap is quick, so a reader sees the whole old file or the whole new file — never a torn one.

**Threads colliding -> a critical section.** In SIMPL# Pro, event handlers fire on **different threads**. Two events could both save at once, or one reads while another writes -> corruption or an `IOException`. Fix: one `CCriticalSection` guards every read and write.

Show the pattern and stress the `try/finally`:
```csharp
_fileLock.Enter();
try { /* read or write */ }
finally { _fileLock.Leave(); }   // ALWAYS, even on exception
```
**Great live demo:** throw an exception between `Enter` and `Leave` *without* a finally, show the deadlock on the next save, then add the finally and show it fixed.

---

## Module 8 — Capstone: the config manager (remainder)

**Goal:** tie every concept into one artifact they take home. Walk through `ConfigManager.cs`.

Point out how one `Save()` method handles **both** failure modes at once (lock + temp-then-swap), and how `Load()` always returns a usable object (first-run default, null guard, catch-and-fall-back). If time allows, build-along: define the class, save it, read it back, add a preset to the array, save again — driven live with `roomconfig` / `roomreload`.

---

## Delivery principles

- **Drive the demos from console commands** (`roomconfig`, `roomreload`) and `ErrorLog`. Prep the VC-4 before class so there's no dead air.
- **Break things on purpose** — delete a comma, delete the file, drop the `finally`. The failure cases are the most memorable teaching. (Bonus: a comment or trailing comma in the file makes System.Text.Json throw, which shows the syntax rules are real.)
- **Hand out the cheat sheet.** Beginners cling to a reference card.
- Keep folder name = project name = namespace so nothing drifts (`CrestronMasters.JsonFileOps`).

## Prerequisites to set up before class

- **One SIMPL# Pro control-system project targeting .NET 8.** No separate demo project; no serialization NuGet package (System.Text.Json is built in).
- **A VC-4 target** with the program deployed — a physical CP4 doesn't run .NET 8 yet, so .NET 8 means VC-4 (or SSH remote-attach debugging).
- **Console commands registered** (`roomconfig`, `roomreload`) so the live demos have something to drive.
- **Verify before class:** deploy to VC-4 and confirm the program loads and round-trips (write a default, read it back) on the actual target.
