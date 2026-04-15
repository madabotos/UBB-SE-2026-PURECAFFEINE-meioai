# Final comprehensive fix script
param()

$root = 'c:\Users\cretu\Desktop\Semester4\ISS\UBB-SE-2026-PURECAFFEINE-meioai'

$files = Get-ChildItem -Path $root -Recurse -Include '*.cs' |
         Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

function Apply-Fixes {
    param([string]$content)

    # ── Fix property access: .id -> .Id (PascalCase property access) ──────────
    # Use word boundary on right side to avoid .id matching inside longer words
    $content = $content -creplace '\.id\b', '.Id'

    # ── Fix property access: .relatedRequestId -> .RelatedRequestId ───────────
    $content = $content -creplace '\.relatedRequestId\b', '.RelatedRequestId'

    # ── Fix object initializer: relatedRequestId = -> RelatedRequestId = ─────
    $content = $content -creplace '\brelatedRequestId\s*=', 'RelatedRequestId ='

    # ── Fix IMapper method names: ToDataTransferObject -> ToDTO ───────────────
    $content = $content -creplace '\bToDataTransferObject\b', 'ToDTO'

    # ── Fix remaining xxxByIdentifier method names -> xxxById ─────────────────
    $content = $content -creplace '\bByIdentifier\b', 'ById'

    # ── Fix named constants that contain Identifier ────────────────────────────
    # NewEntityIdentifier -> NewEntityId
    $content = $content -creplace '\bNewEntityIdentifier\b',          'NewEntityId'
    # MissingUserIdentifier -> MissingUserId
    $content = $content -creplace '\bMissingUserIdentifier\b',        'MissingUserId'
    # MissingForeignKeyIdentifier -> MissingForeignKeyId
    $content = $content -creplace '\bMissingForeignKeyIdentifier\b',  'MissingForeignKeyId'
    # MissingOptionalDatePart stays (not an Identifier)
    # MissingOwnerIdentifier -> MissingOwnerId
    $content = $content -creplace '\bMissingOwnerIdentifier\b',       'MissingOwnerId'
    # InvalidUserIdentifier -> InvalidUserId
    $content = $content -creplace '\bInvalidUserIdentifier\b',        'InvalidUserId'
    # DefaultUserIdentifier -> DefaultUserId
    $content = $content -creplace '\bDefaultUserIdentifier\b',        'DefaultUserId'
    # MinimumSuccessfulEntityIdentifier -> MinimumSuccessfulEntityId
    $content = $content -creplace '\bMinimumSuccessfulEntityIdentifier\b', 'MinimumSuccessfulEntityId'
    # InvalidNotificationIdentifier -> InvalidNotificationId
    $content = $content -creplace '\bInvalidNotificationIdentifier\b', 'InvalidNotificationId'
    # MinimumValidNotificationIdentifier -> MinimumValidNotificationId
    $content = $content -creplace '\bMinimumValidNotificationIdentifier\b', 'MinimumValidNotificationId'
    # MissingIdentifier -> MissingId (if any remain)
    $content = $content -creplace '\bMissingIdentifier\b',            'MissingId'
    # excludeUserIdentifier -> excludeUserId (if any remain in UserService)
    $content = $content -creplace '\bexcludeUserIdentifier\b',        'excludeUserId'
    # incomingGameIdentifier -> incomingGameId
    $content = $content -creplace '\bincomingGameIdentifier\b',       'incomingGameId'
    # parsedIdentifier -> parsedId
    $content = $content -creplace '\bparsedIdentifier\b',             'parsedId'

    # ── Fix: "user.id != excludeUserIdentifier" - user property is now .Id ───
    # (already handled by .id -> .Id above, and excludeUserIdentifier -> excludeUserId)

    # ── Fix: sourceDataTransferObject -> sourceDTO (in IMapper) ──────────────
    $content = $content -creplace '\bsourceDTO\b',                    'sourceDTO'  # already done, idempotent

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
