Option Explicit
Dim shell, fso, folder, executable, command
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
executable = fso.BuildPath(folder, "Snow1970.exe")
command = "powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File " & Chr(34) & fso.BuildPath(folder, "Start.ps1") & Chr(34)
shell.Run command, 0, False
