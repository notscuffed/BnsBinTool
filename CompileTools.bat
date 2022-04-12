SET outputDirectory=%~dp0\Tools

:: SET args=-p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true
SET args=--no-self-contained

dotnet publish -r win-x64 -c Release -o "%outputDirectory%" %args% Src/Tools/BnsBinTool.StringDumper
dotnet publish -r win-x64 -c Release -o "%outputDirectory%" %args% Src/Tools/BnsBinTool.BinDumper
dotnet publish -r win-x64 -c Release -o "%outputDirectory%" %args% Src/Tools/BnsBinTool.DefsToSharp
dotnet publish -r win-x64 -c Release -o "%outputDirectory%" %args% Src/Tools/BnsBinTool.Info
dotnet publish -r win-x64 -c Release -o "%outputDirectory%" %args% Src/Tools/BnsBinTool.Xml

cd Tools

move BnsBinTool.StringDumper.exe bnsds.exe
move BnsBinTool.StringDumper.pdb bnsds.pdb

move BnsBinTool.BinDumper.exe bnsdb.exe
move BnsBinTool.BinDumper.pdb bnsdb.pdb

move BnsBinTool.DefsToSharp.exe bnsd2s.exe
move BnsBinTool.DefsToSharp.pdb bnsd2s.pdb

move BnsBinTool.Info.exe bnsinfo.exe
move BnsBinTool.Info.pdb bnsinfo.pdb

move BnsBinTool.Xml.exe bnsxml.exe
move BnsBinTool.Xml.pdb bnsxml.pdb