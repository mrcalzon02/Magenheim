# Verifies Magenheim's literal reflection bindings against the installed game assemblies.
#
# verify-patch-targets.ps1 only covers [HarmonyPatch] declarations. Member lookups made
# through AccessTools or System.Type carry no compile-time contract, so a Valheim update
# that changes a private member's name or signature breaks them silently at gameplay time.
# Valheim 1.0.12 turned Inventory.Changed() into Changed(bool, bool); three call sites kept
# invoking it with no arguments and threw TargetParameterCountException mid-transaction.
#
# This gate reads the compiled IL, recovers every binding whose target type and member name
# are literals, and requires each to resolve in the installed assemblies. A method bound
# without an explicit parameter-type array must resolve to a single parameterless method, so
# arity drift fails the build instead of reaching a player's inventory.
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
$dynamic = 0
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

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error -Message $_ -ErrorAction Continue }
    throw "Reflection binding verification failed for $($failures.Count) call site(s)."
}
Write-Output "Verified $verified literal reflection bindings against installed assemblies ($dynamic resolved dynamically and not statically checkable)."
