using System.Linq;
using ChatCommandAPI.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace ChatCommandAPI.ServerCommands;

#if DEBUG
public class ExampleShipLoot : ServerCommand
{
    public override string Name => "ShipLoot";

    public override string Description => "Displays the value of all loot on the ship";

    public override void Invoke(PlayerControllerB caller, string args)
    {
        var ship = GameObject.Find("/Environment/HangarShip");
        if (ship == null)
            throw new CommandException("Ship not found");

        Chat.Print(caller, $"Ship: {ShipLoot(ship)}");
    }

    public static int ShipLoot(GameObject ship)
    {
        return ship.GetComponentsInChildren<GrabbableObject>()
            .Where(obj => obj.itemProperties.isScrap && obj is not RagdollGrabbableObject)
            .Sum(scrap => scrap.scrapValue);
    }
}
#endif
