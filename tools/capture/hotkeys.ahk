#Requires AutoHotkey v2.0
#SingleInstance Force
; D4 Build Forge capture hotkeys:
;   Right Shift + F12 -> captures\inbox  (gear / stat panels)
;   Right Shift + F11 -> captures\hits   (dummy damage moments)

INBOX := "C:\sources\BW.D4.BuildForge\captures\inbox"
HITS  := "C:\sources\BW.D4.BuildForge\captures\hits"
SHOT  := A_ScriptDir "\shot.ps1"

>+F12::Snap(INBOX)
>+F11::Snap(HITS)

Snap(dir) {
    Run('powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "' SHOT '" -Dir "' dir '"', , "Hide")
    ToolTip("saved -> " dir)
    SetTimer(() => ToolTip(), -1200)
}
