# Fix-up script: corrects issues from the first rename pass
# Fixes lowercase property names and self-assignment constructor bugs
param()

$root = 'c:\Users\cretu\Desktop\Semester4\ISS\UBB-SE-2026-PURECAFFEINE-meioai'

$files = Get-ChildItem -Path $root -Recurse -Include '*.cs' |
         Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

function Apply-Fixes {
    param([string]$content)

    # ── Fix 1: lowercase property declarations -> PascalCase ──────────────────
    # "public int id {" -> "public int Id {"
    $content = $content -creplace '(\bpublic\s+int\s+)id(\s*\{)', '${1}Id${2}'
    # "public int? relatedRequestId {" -> "public int? RelatedRequestId {"
    $content = $content -creplace '(\bpublic\s+int\?\s+)relatedRequestId(\s*\{)', '${1}RelatedRequestId${2}'

    # ── Fix 2: self-assignment in constructors: "id = id;" -> "this.Id = id;" ──
    # Matches exactly "            id = id;" style lines (assignment start of line)
    $content = $content -creplace '(?m)^(\s*)id\s*=\s*id\s*;', '${1}this.Id = id;'
    # Same for relatedRequestId = relatedRequestId;
    $content = $content -creplace '(?m)^(\s*)relatedRequestId\s*=\s*relatedRequestId\s*;', '${1}this.RelatedRequestId = relatedRequestId;'

    # ── Fix 3: property accesses that are now lowercase need to be PascalCase ──
    # .id -> .Id (property access)
    $content = $content -creplace '\.id\b', '.Id'
    # .relatedRequestId -> .RelatedRequestId (property access)  
    $content = $content -creplace '\.relatedRequestId\b', '.RelatedRequestId'

    # ── Fix 4: IEntity and IDTO property declarations ─────────────────────────
    $content = $content -creplace '(\bint\s+)id(\s*\{\s*get)', '${1}Id${2}'

    # ── Fix 5: Object initializers: "Id = ..." was fine, but "id = ..." in
    #    object initializers (not self-assignments) should also be "Id = ..."
    # Match "id = <expr>" inside object initializer blocks (not after "this.")
    # These look like:   id = someValue,   or   id = someValue
    # This is tricky - let's do it conservatively: any assignment "id = " that
    # is NOT "this.Id = " and NOT a local variable declaration
    # Pattern: beginning of statement (after whitespace) "id = " not preceded by "this."
    $content = $content -creplace '(?m)^(\s+)id(\s*=\s*)(?!id\b)', '${1}Id${2}'

    # ── Fix 6: Int parameter named "id" passed as SqlCommand value ────────────
    # These are fine as-is (local variable named "id")

    return $content
}

$totalChanged = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content) { continue }

    $updated = Apply-Fixes $content

    if ($updated -ne $content) {
        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        $totalChanged++
        Write-Host "  Fixed: $($file.Name)"
    }
}

Write-Host ""
Write-Host "Done. $totalChanged files fixed."
