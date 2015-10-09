﻿namespace Questor.Modules.States
{
    public enum CourierMissionCtrlState
    {
        GotoPickupLocation,
        PickupItem,
        GotoDropOffLocation,
        DropOffItem,
        Idle,
        CompleteMission,
        Done,
        Error
    }
}