# JSON & File Ops — Cheat Sheet
### Programming in C# for Beginners · Crestron Masters

**JSON in one line:** a text format for structured data that lives *outside* your code — readable, editable, shareable.

---

## Class flow

1. Why store data outside code
2. What JSON is
3. JSON vs XML vs others
4. The building blocks + gotchas
5. JSON -> C# classes
6. Serialize / deserialize
7. File read / write
8. The two failure modes
9. Capstone: the config manager
10. Recap

---

## The 6 JSON value types

| Type | Example |
|---|---|
| string | `"Boardroom A"` (always **double** quotes) |
| number | `2` or `19.5` |
| boolean | `true` / `false` (lowercase) |
| null | `null` |
| object | `{ "key": value }` |
| array | `[ 1, 2, 3 ]` |

## Syntax rules (the gotchas)

- Keys and strings use **double quotes** — never single quotes.
- **No trailing comma** after the last item.
- **No comments** in strict JSON.
- Booleans are **lowercase**: `true`, not `True`.

> System.Text.Json enforces all of these by default and is **case-sensitive** on property names.

---

## JSON <-> C# mapping

| JSON | C# |
|---|---|
| object `{ }` | class |
| key | property |
| array `[ ]` | `List<T>` |
| nested object | nested class |

```csharp
using System.Text.Json.Serialization;

public class RoomConfig
{
    [JsonPropertyName("roomName")]   // "roomName" (JSON) <-> RoomName (C#)
    public string RoomName { get; set; }

    [JsonPropertyName("displayCount")]
    public int DisplayCount { get; set; }

    [JsonPropertyName("displays")]
    public List<DisplayInfo> Displays { get; set; }
}
```
> JSON is usually **camelCase**, C# is **PascalCase**. `[JsonPropertyName]` bridges them.

---

## The two words

- **Serialize** = object -> JSON text (write out)
- **Deserialize** = JSON text -> object (read in)

```csharp
using System.Text.Json;

// object -> text
string json = JsonSerializer.Serialize(room,
    new JsonSerializerOptions { WriteIndented = true });

// text -> object
RoomConfig room = JsonSerializer.Deserialize<RoomConfig>(json);
```

## File read / write

```csharp
File.WriteAllText(path, json);          // save
string json = File.ReadAllText(path);   // load
```
**Workflow:** read -> deserialize -> use · change -> serialize -> write back.

---

## Two failure modes to defend against

**1. Power loss mid-write -> write to temp, then swap**
```csharp
File.WriteAllText(tempPath, json);   // write next to the real file
if (File.Exists(path)) File.Delete(path);
File.Move(tempPath, path);           // quick swap — never a half file
```

**2. Threads colliding -> a critical section**
```csharp
private readonly CCriticalSection _fileLock = new CCriticalSection();

_fileLock.Enter();
try   { /* every read AND write of the file goes here */ }
finally { _fileLock.Leave(); }        // ALWAYS release, even on exception
```

## Golden rules

- A bad config file should **never brick a room** — `try/catch` and fall back to a default.
- On first run the file won't exist — **generate a default** instead of throwing.
- Use the **same lock** for reads and writes so a load can't race a save.
- On a processor, use Crestron's file/path helpers — not Windows paths.

---

## System.Text.Json vs Newtonsoft (quick call)

- **Use System.Text.Json (this project):** built into .NET 8, no dependency, strict by default. Perfect for a fixed-shape config.
- **Reach for Newtonsoft when you need:** lenient reading of hand-edited JSON (comments, trailing commas), `PopulateObject` to merge into an existing object, `ShouldSerialize` conditional output, `$type` polymorphism, or its deeper LINQ-to-JSON.
