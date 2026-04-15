# Fix remaining lowercase 'id' property to 'Id' in DTO files
param()

$root = 'c:\Users\cretu\Desktop\Semester4\ISS\UBB-SE-2026-PURECAFFEINE-meioai'

$files = Get-ChildItem -Path $root -Recurse -Include '*.cs' |
         Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$totalChanged = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content) { continue }

    # Fix "public int id {" -> "public int Id {"
    $updated = $content -creplace 'public int id \{', 'public int Id {'

    if ($updated -ne $content) {
        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        $totalChanged++
        Write-Host "  Fixed: $($file.Name)"
    }
}

Write-Host "Done. $totalChanged files fixed."
