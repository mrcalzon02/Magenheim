# Verifies Magenheim's reflection bindings against the installed game assemblies.
#
# verify-patch-targets.ps1 only covers [HarmonyPatch] declarations. Member lookups made
# through AccessTools or System.Type carry no compile-time contract, so a Valheim update
# that changes a private member's name or signature breaks them silently at gameplay time.
# Valheim 1.0.12 turned Inventory.Changed() into Changed(bool, bool); three call sites kept
# invoking it with no arguments and threw TargetParameterCountException mid-transaction.
#
# This gate reads the compiled IL, recovers direct bindings whose target type and member name
# are literals, and also verifies the field contracts intentionally routed through Magenheim
# helper wrappers. A method bound without an explicit parameter-type array must resolve to a
# single parameterless method, so arity drift fails the build instead of reaching a player.
param([string]$RuntimeDll, [string]$GameManagedPath, [string]$BepInExPath)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $BepInExPath 'core/Mono.Cecil.dll')

$runtime = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($RuntimeDll)
$searchModules = @($runtime.MainModule)
foreach ($file in Get-ChildItem -LiteralPath $GameManagedPath -Filter *.dll) {
    try { $searchModules += [Mono.Cecil.AssemblyDefinition]::ReadAssembly($file.FullName).MainModule } catch { }
}

function Resolve-TargetType([string]$fullName) {
    foreach ($module in $searchModules) {
        $type = $module.GetType($fullName)
        if ($type) { return $type }
    }
    return $null
}

# Walks the members of a type and its base chain; private members are not inherited by
# reflection, but AccessTools.Field/Method deliberately search base types.
function Get-Members($type, [string]$kind) {
    $members = @()
    $current = $type
    while ($current) {
        if ($kind -eq 'field') { $members += @($current.Fields) } else { $members += @($current.Methods) }
        if (-not $current.BaseType) { break }
        try { $current = $current.BaseType.Resolve() } catch { break }
    }
    return $members
}

$verified = 0
$helperVerified = 0
$dynamic = 0
$optionalMissing = 0
$failures = @()

foreach ($type in $runtime.MainModule.Types) {
    foreach ($method in @($type.Methods) + @($type.NestedTypes | ForEach-Object { $_.Methods })) {
        if (-not $method.HasBody) { continue }
        foreach ($instruction in $method.Body.Instructions) {
            if ($instruction.OpCode.Name -ne 'call' -and $instruction.OpCode.Name -ne 'callvirt') { continue }
            if (-not $instruction.Operand) { continue }
            $callee = $instruction.Operand.ToString()
            $kind = $null
            if ($callee -match 'AccessTools::Field\(|System\.Type::GetField\(') { $kind = 'field' }
            elseif ($callee -match 'AccessTools::Method\(|System\.Type::GetMethod\(') { $kind = 'method' }
            if (-not $kind) { continue }

            # Recover the literal operands: ldtoken <TargetType> ... ldstr <member> ... [ldtoken <sig>]*
            $memberName = $null; $targetTypeName = $null; $signature = @()
            $walk = $instruction.Previous
            $seenName = $false
            for ($step = 0; $step -lt 40 -and $walk; $step++) {
                if ($walk.OpCode.Name -eq 'ldstr' -and -not $seenName) {
                    $memberName = [string]$walk.Operand; $seenName = $true
                }
                elseif ($walk.OpCode.Name -eq 'ldtoken') {
                    if ($seenName) { $targetTypeName = $walk.Operand.FullName; break }
                    $signature = ,$walk.Operand.FullName + $signature
                }
                $walk = $walk.Previous
            }

            $site = "$($type.Name)::$($method.Name)"
            if (-not $memberName -or -not $targetTypeName) { $dynamic++; continue }

            $targetType = Resolve-TargetType $targetTypeName
            # A type that is absent from the game and from Magenheim belongs to an optional
            # foreign mod resolved at runtime; those bindings are guarded by null checks.
            if (-not $targetType) { $dynamic++; continue }

            $candidates = @(Get-Members $targetType $kind | Where-Object { $_.Name -ceq $memberName })
            if ($candidates.Count -eq 0) {
                $failures += "$site -> $targetTypeName.$memberName ($kind) does not exist in the installed assemblies."
                continue
            }

            if ($kind -eq 'method') {
                if ($signature.Count -gt 0) {
                    $wanted = $signature -join ','
                    $matched = @($candidates | Where-Object {
                        (@($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -ceq $wanted
                    })
                    if ($matched.Count -ne 1) {
                        $failures += "$site -> $targetTypeName.$memberName($wanted) matched $($matched.Count) installed methods."
                        continue
                    }
                }
                else {
                    if ($candidates.Count -ne 1) {
                        $failures += "$site -> $targetTypeName.$memberName is ambiguous ($($candidates.Count) overloads); bind it with an explicit parameter-type array."
                        continue
                    }
                    if ($candidates[0].Parameters.Count -ne 0) {
                        $parameters = (@($candidates[0].Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', ')
                        $failures += "$site -> $targetTypeName.$memberName now takes parameters ($parameters). Bind it with an explicit parameter-type array and pass every argument; reflection never applies declared defaults."
                        continue
                    }
                }
            }
            $verified++
        }
    }
}

# Some runtime helpers intentionally accept the reflected member name as a string argument.
# The direct IL scanner above sees Type.GetField only inside the helper and therefore cannot
# recover the concrete target type/member pair from each caller. Keep those contracts explicit
# here so Valheim API drift is still a build-gated event rather than a manual audit.
#
# Required fields are used by fail-closed helper calls and must exist with the exact type the
# runtime assigns. Optional fields are compatibility enhancements whose absence is tolerated by
# runtime code; the gate reports them without failing so the optional semantics remain truthful.
$helperFieldContracts = @(
    @{
        Owner = 'StaffEffectPayloads.Projectile'
        Type = 'Projectile'
        Required = @{
            m_spawnOnHit = 'UnityEngine.GameObject'
        }
        Optional = @(
            'm_damage', 'm_aoe', 'm_attackForce', 'm_statusEffect',
            'm_spawnOnHitChance', 'm_spawnCount',
            'm_onlySpawnedProjectilesDealDamage', 'm_divideDamageBetweenProjectiles'
        )
    },
    @{
        Owner = 'StaffEffectPayloads.Aoe'
        Type = 'Aoe'
        Required = @{
            m_damage = 'HitData/DamageTypes'
            m_radius = 'System.Single'
            m_ttl = 'System.Single'
            m_hitInterval = 'System.Single'
            m_hitEnemy = 'System.Boolean'
            m_hitCharacters = 'System.Boolean'
        }
        Optional = @(
            'm_damagePerLevel', 'm_useAttackSettings', 'm_scaleDamageByDistance', 'm_ttlMax',
            'm_activationDelay', 'm_attackForce', 'm_dodgeable', 'm_blockable', 'm_hitOwner',
            'm_hitParent', 'm_hitSame', 'm_hitFriendly', 'm_hitProps', 'm_hitTerrain',
            'm_ignorePVP', 'm_skill', 'm_canRaiseSkill', 'm_hitOnEnable', 'm_hitAfterTtl',
            'm_attachToCaster', 'm_statusEffect', 'm_statusEffectIfBoss', 'm_statusEffectIfPlayer'
        )
    },
    @{
        Owner = 'GeodeWorldgenRegistrar.ZNetView'
        Type = 'ZNetView'
        Required = @{
            m_persistent = 'System.Boolean'
        }
        Optional = @()
    },
    @{
        Owner = 'GeodeWorldgenRegistrar.Destructible'
        Type = 'Destructible'
        Required = @{
            m_health = 'System.Single'
            m_minToolTier = 'System.Int32'
        }
        Optional = @()
    },
    @{
        Owner = 'StatusEffects.SE_Stats'
        Type = 'SE_Stats'
        Required = @{
            m_speedModifier = 'System.Single'
        }
        Optional = @()
    }
)

foreach ($contract in $helperFieldContracts) {
    $targetType = Resolve-TargetType $contract.Type
    if (-not $targetType) {
        $failures += "$($contract.Owner) -> required game type '$($contract.Type)' does not exist in the installed assemblies."
        continue
    }

    $fields = @(Get-Members $targetType 'field')
    foreach ($fieldName in $contract.Required.Keys) {
        $matches = @($fields | Where-Object { $_.Name -ceq $fieldName })
        if ($matches.Count -ne 1) {
            $failures += "$($contract.Owner) -> $($contract.Type).$fieldName required helper-wrapped field matched $($matches.Count) installed fields."
            continue
        }
        $expectedType = [string]$contract.Required[$fieldName]
        $actualType = [string]$matches[0].FieldType.FullName
        if ($actualType -cne $expectedType) {
            $failures += "$($contract.Owner) -> $($contract.Type).$fieldName changed type from expected '$expectedType' to '$actualType'."
            continue
        }
        $helperVerified++
    }

    foreach ($fieldName in $contract.Optional) {
        $matches = @($fields | Where-Object { $_.Name -ceq $fieldName })
        if ($matches.Count -eq 1) {
            $helperVerified++
            continue
        }
        $optionalMissing++
        Write-Warning "$($contract.Owner) -> optional $($contract.Type).$fieldName matched $($matches.Count) installed fields; runtime compatibility code will omit that optional assignment."
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error -Message $_ -ErrorAction Continue }
    throw "Reflection binding verification failed for $($failures.Count) call site/contract(s)."
}
Write-Output "Verified $verified direct literal reflection bindings and $helperVerified helper-wrapped field contracts against installed assemblies ($dynamic resolved dynamically and not statically checkable; $optionalMissing optional helper fields absent)."
