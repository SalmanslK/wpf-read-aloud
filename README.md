# Read Aloud

A small Windows desktop app (WPF, .NET 10) that reads text aloud with the built-in Windows voices.

- **Type or paste** any text, or **open a PDF** and load a page range.
- **Speak** starts from the cursor (or the top), and the current word is highlighted as it is read.
- **Pause / Resume / Stop**, choose a **voice** and set the **speed**.

PDF text is extracted with [PdfPig](https://github.com/UglyToad/PdfPig) (Apache-2.0); speech uses `System.Speech`.

```
dotnet run --project ReadAloud
```

Combines two earlier experiments, `wpf-text-to-speech` and `wpf-pdf-read-aloud`.
