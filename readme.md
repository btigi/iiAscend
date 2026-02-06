iiAscend
=========

C# library supporting the modification of files relating to Descent, the 1995 FPS game developed by Parallax Software.


| Name  | Read | Write | Comment |
|-------|:----:|-------|:--------|
| 256   | ✔   |  ✔  |
| BBM   | ✔   |  ✗  | Two variants
| BNK   | ✔   |  ✔  |
| DEM   | ✔   |  ✗  |
| DIG   | ✗   |  ✗  |
| FNT   | ✔   |  ✔  |
| HAM   | ✔   |  ✔  |
| HMP   | ✗   |  ✗  |
| HMQ   | ✗   |  ✗  |
| HOG   | ✔   |  ✔  |
| INI   | ✗   |  ✗  | Plain text
| MID   | ✔   |  ✗  | 
| MN2   | ✔   |  ✔  |
| MSN   | ✔   |  ✔  | Plain text
| MVL   | ✔   |  ✔  |
| PCX   | ✔   |  ✗  |
| PIG   | ✔   |  ✗  | D1, D2
| POF   | ✔   |  ✔  |
| RAW   | ✔   |  ✗  |
| RDL   | ✔   |  ✔  |
| RL2   | ✗   |  ✗  |
| S11   | ✔   |  ✔  | 
| S22   | ✔   |  ✔  |
| SNG   | ✔   |  ✔  | Plain text
| TXB   | ✔   |  ✔  |

## Usage

Install the [nuget package](https://www.nuget.org/packages/ii.Ascend/) e.g.

`dotnet add package ii.Ascend`

To edit a file you should instantiate the relevant class and call the `Read` method passing the filename. This will return an object model, which you can amend, before calling the `Write` method.

```csharp
var hogProcessor = new HogProcessor();
var files = hogProcessor.Read(@"D:\Games\Descent2\DESCENT2.HOG");

foreach (var (filename, bytes) in files)
{
    var outputPath = Path.Combine(@"D:\data\descent", filename);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await File.WriteAllBytesAsync(outputPath, bytes);
    Console.WriteLine($"Extracted {filename} ({bytes.Length} bytes)");
}

var txbProcessor = new TxbProcessor();
var text = txbProcessor.Read(@"D:\data\descent\credits.txb");

var demProcessor = new DemProcessor();
var demo = demProcessor.Read(@"D:\data\descent\demo1.dem");
Console.WriteLine($"Demo version: {demo.Version}, Game type: {demo.GameType}");
Console.WriteLine($"Total events: {demo.Events.Count}");
foreach (var evt in demo.Events.Take(10))
{
    Console.WriteLine($"Event type: {evt.EventType}");
}
```

## Compiling

To clone and run this repository you'll need [Git](https://git-scm.com) and [.NET](https://dotnet.microsoft.com/) installed on your computer. From your command line:

```
# Clone this repository
$ git clone https://github.com/btigi/iiAscend

# Go into the repository
$ cd src

# Build  the app
$ dotnet build
```

## Licencing

iiAscend is licenced under the MIT License. Full licence details are available in licence.md