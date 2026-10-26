using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTrainUnitCommand", menuName = "AgeOfTheBrick/Commands/Train Unit Command")]
public class TrainUnitCommandSO : CommandSO
{
    [Header("Unit Data Reference")]
    public UnitStats unitToTrain;

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        Player owner = PlayerManager.Instance?.GetPlayer(context.ownerPlayerId);

        
        string unitName = unitToTrain != null && owner != null ? unitToTrain.unitName : "Unit";
        Sprite unitIcon = unitToTrain != null && owner != null ? unitToTrain.statsPerEra[owner.CurrentEra].image : null;

        return new CommandAction
        {
            id = $"train_{unitName.ToLower()}",
            displayName = $"Train {unitName}",
            description = unitToTrain != null ? unitToTrain.description : string.Empty,
            icon = unitIcon,
            buttonType = EActionButtonType.Standard,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () =>
            {
                if (context == null || context.state != SelectableState.Alive) return false;
                return owner != null && owner.HasResourcesForUnit(unitToTrain);
            },
            actionCallback = () =>
            {
                if (context.TryGetComponent(out UnitProducerComponent producer))
                {
                    producer.QueueUnit(unitToTrain);
                }
            }
        };
    }
}