# music id trainer

small, open-source, offline Windows app for practicing acadec music. load ur own recordings, then practice in either mode:

- **identify pieces:** hear an excerpt from a random recording and name the piece.
- **listening guide:** hear an excerpt at a question's timestamp, then answer a multiple-choice question from a JSON pack.

choose the clip length, replay a clip, and see your score. no login or internet connection required hehe

## download and play

open **Releases** on this repository's GitHub page and download **mIDtdark.exe** (maroon with twilight sparkle) or **mIDtlight.exe** (original Windows theme). theyre separate, single-file Windows x64 apps. you do not need to install anything else

download [qpack.json](qpack.json) from the repository, or use your own question pack. run either app, load the [MP3 recordings](https://drive.google.com/drive/u/0/folders/1F3jt0fHNH3tIe5bMJ4hF5EjAPXcarFNQ) (request access and i will grant you), and then load the question pack for **listening guide** mode. the included pack has 70 questions painstakingly made covering the 14 pieces from the 2026–27 usad listening guides; you can use the **identify pieces** mode without any pack. recordings are supplied by the user and everything is local.

if Windows warns about an unrecognized app, thats cuz these community builds have not been code signed. the source for each executable is below. if you for some reason cannot download the files, try the [web version](https://drive.google.com/drive/u/0/folders/1_D7seUYNzQsv89nLyTEZZl_4Q8h4wbJX). 


# hi

i did acadec in 2025-2026 at phs and this is my way of giving back. i loved the program so much more than i ever thought i would and i hope this can be of use. i've been working on this slowly for a few months on-and-off after i had the idea in february. ofc its open source so you can use it for literally whatever you want but yeah..



# source and rebuilding

the `dark/` and `light/` folders contain the complete C# source for each theme. the project uses Windows Forms for the interface and Windows media APIs for audio. the dark version has twilight sparkle `dark/Assets/companion.gif` in its executable.

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows. In PowerShell, run these commands from the repository's root:

```powershell
dotnet publish dark/MusicIdTrainer.csproj -c Release -r win-x64 --self-contained true -p:AssemblyName=mIDtdark -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o dist/dark
dotnet publish light/MusicIdTrainer.csproj -c Release -r win-x64 --self-contained true -p:AssemblyName=mIDtlight -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o dist/light
```

The resulting programs are `dist/dark/mIDtdark.exe` and `dist/light/mIDtlight.exe`. The bundled .NET runtime makes each executable larger, but there are no other files to keep beside it. GIF artwork remains the creator's property; the [MIT license](LICENSE) covers the app source and original question pack, not third-party artwork or recordings.

## question pack format

app reads a version 1 JSON object with `format`, `version`, `title`, and `cards`. every card contains a recording `file` (including `.mp3`), a `start` time in seconds, `question`, five `choices`, a zero-based `answer` index, and an `explanation`. see [qpack.json](qpack.json) for complete examples. remember only recordings with matching filenames are included in listening-guide practice! i made this very easy to expand so mr thacker can lock in
