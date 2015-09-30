namespace Questor.Modules.States
{
    public enum StorylineState
    {
        Idle,
        Arm,
        GotoAgent,
        PreAcceptMission,
        DeclineMission,
        AcceptMission,
        ExecuteMission,
        CompleteMission,
        Done,
        BlacklistAgent,
        BringSpoilsOfWar,
        ReturnToAgent
    }
}