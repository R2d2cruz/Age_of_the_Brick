using UnityEngine;

/// <summary>
/// Command used to place a building in the world.
/// Creates a runtime action that starts the building placement workflow
/// for builder units.
/// </summary>
[CreateAssetMenu(
    fileName = "NewBuildCommand",
    menuName = "AgeOfTheBrick/Commands/Build Command")]
public class BuildBuildingCommandSO : CommandSO
{
    [Header("Building Data Reference")]

    /// <summary>
    /// Building type that will be placed when the command is executed.
    /// </summary>
    public BuildingStats buildingToBuild;

    /// <summary>
    /// Creates a runtime command action for the specified context.
    /// </summary>
    /// <param name="context">
    /// Entity requesting the command.
    /// </param>
    /// <returns>
    /// Runtime command configured for the selected builder unit.
    /// </returns>
    public override CommandAction CreateRuntimeAction(Selectable context)
    {
        Player owner =
            PlayerManager.Instance?.GetPlayer(context.ownerPlayerId);

        // Extract display information from the building definition.
        string buildingName =
            buildingToBuild != null
                ? buildingToBuild.buildingName
                : "Building";

        Sprite buildingIcon =
            buildingToBuild != null &&
            owner != null && owner.CurrentEra < buildingToBuild.statsPerEra.Length
                ? buildingToBuild.statsPerEra[owner.CurrentEra].image
                : null;

        return new CommandAction
        {
            id = $"build_{buildingName.ToLower()}",
            displayName = buildingName,
            description = buildingToBuild != null
                ? buildingToBuild.description
                : string.Empty,
            icon = buildingIcon,
            buttonType = EActionButtonType.Standard,
            gridIndex = defaultGridIndex,
            hotkey = defaultHotkey,

            isInteractable = () =>
            {
                if (context == null ||
                    context.state != SelectableState.Alive)
                    return false;

                // Verify that the player has enough resources
                // to construct the building.
                return owner != null &&
                       owner.HasResourcesForBuilding(buildingToBuild);
            },

            actionCallback = () =>
            {
                if (context is Unit unit)
                {
                    if (unit.TryGetComponent(out IBuilder builder))
                    {
                        builder.StartPlacingBuilding(buildingToBuild);
                    }
                }
            }
        };
    }
}