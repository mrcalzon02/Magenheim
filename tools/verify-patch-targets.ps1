param([string]$RuntimeDll, [string]$GameManagedPath, [string]$BepInExPath)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $BepInExPath 'core/Mono.Cecil.dll')
# Cecil needs to resolve an attribute argument's declaring assembly before it can decode an
# enum-typed value. Without 0Harmony.dll on the search path a HarmonyPatch attribute carrying an
# ArgumentType[] decodes as zero constructor arguments, which surfaced as a misleading
# "unsupported patch declaration" rather than as the unreadable blob it actually was.
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
foreach ($directory in @((Split-Path -Parent $RuntimeDll), $GameManagedPath, (Join-Path $BepInExPath 'core'))) {
    if (Test-Path -LiteralPath $directory) { $resolver.AddSearchDirectory($directory) }
}
$readerParameters = New-Object Mono.Cecil.ReaderParameters
$readerParameters.AssemblyResolver = $resolver
$runtimeAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($RuntimeDll, $readerParameters)
$gameAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameManagedPath 'assembly_valheim.dll'), $readerParameters)
try {
    $count = 0
    foreach ($type in $runtimeAssembly.MainModule.Types) {
        foreach ($provider in @($type) + @($type.Methods)) {
            foreach ($attribute in $provider.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }) {
                $arguments = $attribute.ConstructorArguments
                if ($arguments.Count -lt 2 -or $arguments[0].Type.FullName -ne 'System.Type' -or
                    $arguments[1].Type.FullName -ne 'System.String') { throw "Unsupported patch declaration on $($provider.FullName)." }
                $targetName = $arguments[0].Value.FullName
                $methodName = [string]$arguments[1].Value
                $target = $gameAssembly.MainModule.GetType($targetName)
                if (!$target) { $target = $runtimeAssembly.MainModule.GetType($targetName) }
                if (!$target) { throw "Missing patch type: $targetName" }
                $candidates = @($target.Methods | Where-Object Name -eq $methodName)
                if ($arguments.Count -gt 2) {
                    if ($arguments[2].Type.FullName -ne 'System.Type[]') { throw 'Unsupported patch signature format.' }
                    $declared = @($arguments[2].Value | ForEach-Object { $_.Value.FullName })
                    # An attribute cannot carry a by-ref Type, so Harmony expresses ref/out/pointer
                    # parameters in a parallel ArgumentType[]. Without decoding it, any target with
                    # an out-parameter resolves to zero methods and looks like patch drift.
                    if ($arguments.Count -gt 3) {
                        if ($arguments[3].Type.FullName -ne 'HarmonyLib.ArgumentType[]') { throw 'Unsupported patch argument-type format.' }
                        $kinds = @($arguments[3].Value | ForEach-Object { [int]$_.Value })
                        if ($kinds.Count -ne $declared.Count) {
                            throw "Patch $($provider.FullName) declares $($declared.Count) argument types and $($kinds.Count) argument kinds."
                        }
                        for ($i = 0; $i -lt $declared.Count; $i++) {
                            switch ($kinds[$i]) {
                                0 { }
                                1 { $declared[$i] = $declared[$i] + '&' }
                                2 { $declared[$i] = $declared[$i] + '&' }
                                3 { $declared[$i] = $declared[$i] + '*' }
                                default { throw "Unsupported Harmony argument kind $($kinds[$i]) on $($provider.FullName)." }
                            }
                        }
                    }
                    $signature = $declared -join ','
                    $candidates = @($candidates | Where-Object {
                        (@($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -ceq $signature
                    })
                }
                if ($candidates.Count -ne 1) { throw "Patch $($provider.FullName) resolves to $($candidates.Count) methods: $targetName.$methodName. Specify the exact installed-game signature." }
                $targetMethod = $candidates[0]
                $patchMethods = if ($provider -is [Mono.Cecil.TypeDefinition]) {
                    @($provider.Methods | Where-Object Name -in @('Prefix','Postfix'))
                } else { @($provider) }
                foreach ($patchMethod in $patchMethods) {
                    foreach ($parameter in $patchMethod.Parameters) {
                        if ($parameter.Name.StartsWith('__')) { continue }
                        $originalParameter = @($targetMethod.Parameters | Where-Object Name -eq $parameter.Name)
                        if ($originalParameter.Count -ne 1) { throw "Unbound patch parameter $($parameter.Name) in $($patchMethod.FullName)." }
                        if ($parameter.ParameterType.FullName.TrimEnd('&') -cne $originalParameter[0].ParameterType.FullName.TrimEnd('&')) {
                            throw "Patch parameter type mismatch in $($patchMethod.FullName)."
                        }
                    }
                }
                $count++
            }
        }
    }
    if ($count -eq 0) { throw 'No Harmony targets were inspected.' }
    Write-Output "Verified $count Harmony patch targets and named argument bindings against installed assemblies."
} finally { $runtimeAssembly.Dispose(); $gameAssembly.Dispose() }
