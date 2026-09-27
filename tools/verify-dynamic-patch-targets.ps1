param(
    [Parameter(Mandatory=$true)][string]$RuntimeDll,
    [Parameter(Mandatory=$true)][string]$GameManagedPath,
    [Parameter(Mandatory=$true)][string]$BepInExPath
)
$ErrorActionPreference = 'Stop'

$searchDirectories = @(
    (Split-Path -Parent $RuntimeDll),
    $GameManagedPath,
    (Join-Path $BepInExPath 'core'),
    (Join-Path $BepInExPath 'plugins')
) | Where-Object { Test-Path -LiteralPath $_ }

$handler = [ResolveEventHandler]{
    param($sender, $args)
    try {
        $fileName = ([Reflection.AssemblyName]$args.Name).Name + '.dll'
        foreach ($directory in $searchDirectories) {
            $candidate = Join-Path $directory $fileName
            if (Test-Path -LiteralPath $candidate) {
                return [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $candidate).Path)
            }
        }
    } catch { }
    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
try {
    $runtime = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $RuntimeDll).Path)
    $flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
    $dynamic = 0
    $resolvedCount = 0

    foreach ($type in $runtime.GetTypes()) {
        $hasHarmonyPatch = @($type.GetCustomAttributes($false) | Where-Object {
            $_.GetType().FullName -eq 'HarmonyLib.HarmonyPatch'
        }).Count -gt 0
        if (!$hasHarmonyPatch) { continue }

        $single = $type.GetMethod('TargetMethod', $flags)
        $many = $type.GetMethod('TargetMethods', $flags)
        if (!$single -and !$many) { continue }
        if ($single -and $many) { throw "$($type.FullName) declares both TargetMethod and TargetMethods." }

        $dynamic++
        $targets = New-Object System.Collections.Generic.List[Reflection.MethodBase]
        if ($single) {
            $method = $single.Invoke($null, @())
            if ($null -eq $method -or !($method -is [Reflection.MethodBase])) {
                throw "$($type.FullName).TargetMethod did not resolve a MethodBase."
            }
            $targets.Add([Reflection.MethodBase]$method)
        } else {
            $sequence = $many.Invoke($null, @())
            if ($null -eq $sequence) { throw "$($type.FullName).TargetMethods returned null." }
            foreach ($method in $sequence) {
                if ($null -eq $method -or !($method -is [Reflection.MethodBase])) {
                    throw "$($type.FullName).TargetMethods emitted a non-MethodBase value."
                }
                $targets.Add([Reflection.MethodBase]$method)
            }
        }

        if ($targets.Count -eq 0) { throw "$($type.FullName) resolved zero dynamic Harmony targets." }

        $identities = @{}
        foreach ($method in $targets) {
            $identity = "$($method.Module.ModuleVersionId)|$($method.MetadataToken)"
            if ($identities.ContainsKey($identity)) {
                throw "$($type.FullName) resolved duplicate target $($method.DeclaringType.FullName).$($method.Name)."
            }
            $identities[$identity] = $true
            $resolvedCount++
        }

        Write-Output "Dynamic Harmony resolver $($type.FullName): $($targets.Count) target(s)."
    }

    if ($dynamic -eq 0) { throw 'No dynamic Harmony resolvers were inspected.' }
    Write-Output "Verified $dynamic dynamic Harmony resolver(s), $resolvedCount resolved installed-runtime target(s)."
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($handler)
}
