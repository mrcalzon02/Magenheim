using System;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Magenheim.Runtime.TrueBlacksmithing;

internal enum TrueBlacksmithingModuleState
{
    Disabled,
    Ready,
    Activating,
    Active,
    Faulted
}

/// <summary>
/// Fail-safe boundary for the optional True Blacksmithing subsystem.
/// Magenheim's baseline crafting path remains authoritative unless a complete
/// True Blacksmithing activation succeeds.
/// </summary>
internal sealed class TrueBlacksmithingModule : IDisposable
{
    private readonly ManualLogSource _logger;
    private TrueBlacksmithingMutationJournal? _mutationJournal;
    private TrueBlacksmithingModuleState _state;

    private TrueBlacksmithingModule(
        ManualLogSource logger,
        TrueBlacksmithingModuleState initialState)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _state = initialState;
    }

    internal TrueBlacksmithingModuleState State => _state;

    internal static TrueBlacksmithingModule Create(
        ConfigFile config,
        ManualLogSource logger)
    {
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        try
        {
            TrueBlacksmithingConfig.Configure(config);

            var state = TrueBlacksmithingConfig.Enabled
                ? TrueBlacksmithingModuleState.Ready
                : TrueBlacksmithingModuleState.Disabled;

            if (state == TrueBlacksmithingModuleState.Disabled)
            {
                logger.LogInfo(
                    "True Blacksmithing is disabled; baseline Magenheim/Valheim crafting remains unchanged.");
            }
            else
            {
                logger.LogInfo(
                    "True Blacksmithing is configured on and is waiting for its production activation plan.");
            }

            return new TrueBlacksmithingModule(logger, state);
        }
        catch (Exception exception)
        {
            logger.LogError(
                $"True Blacksmithing configuration failed. The subsystem is faulted and baseline crafting will be preserved: {exception}");

            return new TrueBlacksmithingModule(
                logger,
                TrueBlacksmithingModuleState.Faulted);
        }
    }

    internal bool TryActivate(Action<TrueBlacksmithingMutationJournal> installer)
    {
        if (installer is null) throw new ArgumentNullException(nameof(installer));

        if (_state == TrueBlacksmithingModuleState.Disabled
            || _state == TrueBlacksmithingModuleState.Faulted)
        {
            return false;
        }

        if (_state == TrueBlacksmithingModuleState.Active)
        {
            return true;
        }

        if (_state != TrueBlacksmithingModuleState.Ready)
        {
            return false;
        }

        _state = TrueBlacksmithingModuleState.Activating;
        _mutationJournal = new TrueBlacksmithingMutationJournal(_logger);

        try
        {
            installer(_mutationJournal);
            _state = TrueBlacksmithingModuleState.Active;
            _logger.LogInfo("True Blacksmithing activated successfully.");
            return true;
        }
        catch (Exception exception)
        {
            Fault(
                "activation",
                exception);
            return false;
        }
    }

    internal bool TryExecute(string operationName, Action operation)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("Operation name is required.", nameof(operationName));
        }

        if (operation is null) throw new ArgumentNullException(nameof(operation));

        if (_state != TrueBlacksmithingModuleState.Active)
        {
            return false;
        }

        try
        {
            operation();
            return true;
        }
        catch (Exception exception)
        {
            Fault(operationName, exception);
            return false;
        }
    }

    private void Fault(string boundary, Exception exception)
    {
        _logger.LogError(
            $"True Blacksmithing faulted during {boundary}. "
            + "The subsystem is being disabled for this session and reversible crafting mutations are being rolled back. "
            + exception);

        _mutationJournal?.RollbackAll();
        _mutationJournal = null;
        _state = TrueBlacksmithingModuleState.Faulted;
    }

    public void Dispose()
    {
        _mutationJournal?.RollbackAll();
        _mutationJournal = null;

        if (_state != TrueBlacksmithingModuleState.Faulted)
        {
            _state = TrueBlacksmithingModuleState.Disabled;
        }
    }
}
