param([string]$RuntimeDll, [string]$GameManagedPath, [string]$BepInExPath)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $BepInExPath 'core/Mono.Cecil.dll')
$verifier = Join-Path $PSScriptRoot '../tools/verify-reflection-targets.ps1'
& $verifier @PSBoundParameters
$output = Join-Path $PSScriptRoot '../build/reflection-regressions'
New-Item -ItemType Directory -Force -Path $output | Out-Null

# Mutate copies of the compiled assembly, never the runtime/install payload. A renamed singleton
# and a lost by-ref signature must fail even after fixing the scanner's nested-receiver inference.
foreach ($case in @('missing-singleton', 'wrong-load-signature', 'missing-adapter-singleton')) {
    $resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
    foreach ($directory in @((Split-Path $RuntimeDll -Parent), $GameManagedPath, (Join-Path $BepInExPath 'core'))) {
        $resolver.AddSearchDirectory($directory)
    }
    $parameters = New-Object Mono.Cecil.ReaderParameters
    $parameters.AssemblyResolver = $resolver
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($RuntimeDll, $parameters)
    try {
        if ($case -in @('missing-singleton', 'missing-adapter-singleton')) {
            $typeName = if ($case -eq 'missing-singleton') { 'UnderworldZoneSystemLifetimeGuard' } else { 'ValheimWorldInstanceExecution' }
            $type = $assembly.MainModule.Types | Where-Object Name -eq $typeName
            $method = $type.Methods | Where-Object Name -eq '.cctor'
            $literal = $method.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 's_instance' } | Select-Object -First 1
            if (!$literal) { throw 'Missing singleton mutation site.' }
            $literal.Operand = 'missing_instance_regression'
        } else {
            $type = $assembly.MainModule.Types | Where-Object Name -eq 'UnderworldZoneMetadataLoadFilterPatch'
            $method = $type.Methods | Where-Object Name -eq 'TargetMethod'
            $call = $method.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'callvirt' -and $_.Operand.Name -eq 'MakeByRefType' } | Select-Object -First 1
            if (!$call) { throw 'Missing by-ref mutation site.' }
            $call.OpCode = [Mono.Cecil.Cil.OpCodes]::Nop
            $call.Operand = $null
        }
        $path = Join-Path $output ($case + '.dll')
        $assembly.Write($path)
    } finally { $assembly.Dispose(); $resolver.Dispose() }
    $rejected = $false
    try { & $verifier -RuntimeDll $path -GameManagedPath $GameManagedPath -BepInExPath $BepInExPath *> (Join-Path $output ($case + '.log')) }
    catch { $rejected = $_.Exception.Message -like 'Reflection binding verification failed*' }
    if (!$rejected) { throw "Reflection gate failed to reject $case." }
}
Write-Output 'Reflection regression tests passed: valid bindings accepted; missing lifecycle/adapter singletons and wrong by-ref signature rejected.'
