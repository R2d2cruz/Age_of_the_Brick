using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildCommand", menuName = "AgeOfTheBrick/Commands/Build Command")]
public class BuildBuildingCommandSO : CommandSO
{
    [Header("Building Data Reference")]
    public BuildingStats buildingToBuild; //

    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        Player owner = PlayerManager.Instance?.GetPlayer(context.ownerPlayerId);
        // Extract data directly from BuildingStats asset
        string buildingName = buildingToBuild != null ? buildingToBuild.name : "Building";
        Sprite buildingIcon = buildingToBuild != null ? buildingToBuild.statsPerEra[owner.CurrentEra].image : null;

        return new CommandAction
        {
            id = $"build_{buildingName.ToLower()}",
            displayName = buildingName,
            description = buildingToBuild != null ? buildingToBuild.description : string.Empty,
            icon = buildingIcon,
            buttonType = EActionButtonType.Standard,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,
            isInteractable = () =>
            {
                if (context == null || context.state != SelectableState.Alive) return false;
                
                // Check if the owner player has enough resources using BuildingStats
                return owner != null && owner.HasResourcesForBuilding(buildingToBuild);
            },
            actionCallback = () =>
            {
                if (context is Villager villager)
                {
                    villager.StartPlacingBuilding(buildingToBuild); //[cite: 1]
                }
            }
        };
    }
}