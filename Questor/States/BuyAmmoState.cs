/*
 * ---------------------------------------
 * User: duketwo
 * Date: 09.10.2015
 * Time: 18:36
 * 
 * ---------------------------------------
 */

namespace Questor.Modules.States
{
    public enum BuyAmmoState
    {
        Idle,
        AmmoCheck,
        ActivateTransportShip,
        CreateBuyList,
        TravelToDestinationStation,
        BuyAmmo,
        MoveItemsToCargo,
        TravelToHomeSystem,
        Done,
        Error,
        DisabledForThisSession
    }
}