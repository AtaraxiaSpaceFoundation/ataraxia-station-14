namespace Content.Shared._White.GhostRecruitment;

//this for ghosts
[RegisterComponent]
public sealed partial class GhostRecruitedComponent : Component
{
    [DataField("recruitmentName")]
    public string RecruitmentName = "default";
}
