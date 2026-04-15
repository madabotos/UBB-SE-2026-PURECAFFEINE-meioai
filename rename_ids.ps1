# Rename script v2 - case-sensitive replacements
# Uses -creplace to avoid case-insensitive matching issues
param()

$root = 'c:\Users\cretu\Desktop\Semester4\ISS\UBB-SE-2026-PURECAFFEINE-meioai'

$files = Get-ChildItem -Path $root -Recurse -Include '*.cs' |
         Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "Found $($files.Count) .cs files"

function Apply-Replacements {
    param([string]$content)

    # ── 1. Generic type parameter (PascalCase) ────────────────────────────────
    $content = $content -creplace 'TModelDataTransferObject', 'TDTO'

    # ── 2. Interface / Class TYPE names (PascalCase) ──────────────────────────
    $content = $content -creplace 'IDataTransferObject', 'IDTO'
    $content = $content -creplace 'GameDataTransferObject',         'GameDTO'
    $content = $content -creplace 'UserDataTransferObject',         'UserDTO'
    $content = $content -creplace 'NotificationDataTransferObject', 'NotificationDTO'
    $content = $content -creplace 'RentalDataTransferObject',       'RentalDTO'
    $content = $content -creplace 'RequestDataTransferObject',      'RequestDTO'

    # ── 3. PascalCase property: Identifier -> Id ──────────────────────────────
    # This must run BEFORE the lowercase 'identifier' rule
    # Handles: public int Identifier, .Identifier, Identifier =, = Identifier, { Identifier }, etc.
    $content = $content -creplace '\bIdentifier\b', 'Id'

    # ── 4. camelCase variable / parameter names ending in …identifier ─────────
    # Order: longest compound names first
    $content = $content -creplace '\bnotificationIdentifier\b',   'notificationId'
    $content = $content -creplace '\brelatedRequestIdentifier\b', 'relatedRequestId'
    $content = $content -creplace '\bofferingUserIdentifier\b',   'offeringUserId'
    $content = $content -creplace '\bcancellingUserIdentifier\b', 'cancellingUserId'
    $content = $content -creplace '\btargetUserIdentifier\b',     'targetUserId'
    $content = $content -creplace '\bcurrentUserIdentifier\b',    'currentUserId'
    $content = $content -creplace '\bexcludeRequestIdentifier\b', 'excludeRequestId'
    $content = $content -creplace '\bupdatedEntityIdentifier\b',  'updatedEntityId'
    $content = $content -creplace '\bremovedEntityIdentifier\b',  'removedEntityId'
    $content = $content -creplace '\brenterUserIdentifier\b',     'renterUserId'
    $content = $content -creplace '\bownerUserIdentifier\b',      'ownerUserId'
    $content = $content -creplace '\brenterIdentifier\b',         'renterId'
    $content = $content -creplace '\bownerIdentifier\b',          'ownerId'
    $content = $content -creplace '\bgameIdentifier\b',           'gameId'
    $content = $content -creplace '\brequestIdentifier\b',        'requestId'
    $content = $content -creplace '\brentalIdentifier\b',         'rentalId'
    $content = $content -creplace '\buserIdentifier\b',           'userId'
    # bare lowercase 'identifier' constructor param: replace ONLY when it is
    # used as a standalone identifier (not part of int Id { } which we already fixed)
    $content = $content -creplace '\bidentifier\b',               'id'

    # ── 5. Fix constructor self-assignment: this.Id = id  ─────────────────────
    # After the above, "Id = id;" is wrong (field vs param same name).
    # The only constructors that do this are in the model classes:
    # "Id = id;" should become "this.Id = id;"
    $content = $content -creplace '(?<!\.)(\bId\b)\s*=\s*\bid\b', 'this.Id = id'

    # ── 6. gameOwnerUserMapper -> gameOwnerMapper ─────────────────────────────
    $content = $content -creplace '\bgameOwnerUserMapper\b', 'gameOwnerMapper'

    return $content
}

$totalChanged = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    if ($null -eq $content) { continue }

    $updated = Apply-Replacements $content

    if ($updated -ne $content) {
        Set-Content -Path $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        $totalChanged++
        Write-Host "  Updated: $($file.Name)"
    }
}

Write-Host ""
Write-Host "Done. $totalChanged files modified."
