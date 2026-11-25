using Unity.Netcode;

/// <summary>
/// Flask relay (gear-slots S2). The host owns charges and cooldown for every
/// hero; the owning client applies the heal ticks because it writes its own HP.
/// </summary>
public sealed partial class DiabloNetworkPlayer
{
    /// <summary>Owner → host: drink request. The host validates charges and cooldown.</summary>
    public void RequestFlaskDrink()
    {
        if (!IsSpawned || !IsOwner) return;
        if (IsServer) HealingFlask.For(gameObject).DrinkAuthoritative();
        else SubmitFlaskDrinkServerRpc();
    }

    [ServerRpc]
    void SubmitFlaskDrinkServerRpc()
    {
        var flask = HealingFlask.For(gameObject);
        if (!flask.DrinkAuthoritative()) SendFlaskState(flask.Charges, flask.CooldownRemaining);
    }

    /// <summary>Host → owner: the drink stands, start the heal and mirror the state.</summary>
    public void ConfirmFlaskToOwner(float amount, int charges, float cooldownRemaining)
    {
        if (!IsServer) return;
        FlaskDrunkClientRpc(amount, charges, cooldownRemaining, OwnerOnly());
    }

    /// <summary>Host → owner: charges and cooldown changed without a drink (refill, rejection).</summary>
    public void SendFlaskState(int charges, float cooldownRemaining)
    {
        if (!IsServer || IsOwner) return;
        FlaskStateClientRpc(charges, cooldownRemaining, OwnerOnly());
    }

    ClientRpcParams OwnerOnly() => new ClientRpcParams
    {
        Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } },
    };

    [ClientRpc]
    void FlaskDrunkClientRpc(float amount, int charges, float cooldownRemaining, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner || IsServer) return;
        if (amount <= 0f || amount > HealingFlask.MaxNetHeal) return;
        var flask = HealingFlask.For(gameObject);
        flask.ApplyState(charges, cooldownRemaining);
        flask.BeginHeal(amount);
    }

    [ClientRpc]
    void FlaskStateClientRpc(int charges, float cooldownRemaining, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner || IsServer) return;
        HealingFlask.For(gameObject).ApplyState(charges, cooldownRemaining);
    }

    /// <summary>Host → owner: an owner-side gear effect (heal, haste) earned on the host.</summary>
    public void SendGearEffect(GearEffect effect, float value)
    {
        if (!IsServer || IsOwner) return;
        GearEffectClientRpc((int)effect, value, OwnerOnly());
    }

    [ClientRpc]
    void GearEffectClientRpc(int effect, float value, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner || IsServer) return;
        if (value <= 0f || value > HealingFlask.MaxNetHeal) return;
        if (!System.Enum.IsDefined(typeof(GearEffect), effect)) return;
        GearUniqueEffects.ApplyLocal(gameObject, (GearEffect)effect, value);
    }
}
