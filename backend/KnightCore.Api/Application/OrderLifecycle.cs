namespace KnightCore.Application;
public static class OrderLifecycle
{
    private static readonly Dictionary<string,string[]> Transitions=new()
    {
        ["Confirmed"]=["InProgress","OnHold","Cancelled"],
        ["InProgress"]=["ReadyForReview","OnHold","Cancelled"],
        ["ReadyForReview"]=["InProgress","Delivered","OnHold"],
        ["Delivered"]=["Completed","InProgress"],
        ["OnHold"]=["Confirmed","InProgress","Cancelled"],
        ["Completed"]=[],
        ["Cancelled"]=[]
    };
    public static bool CanMove(string from,string to)=>Transitions.TryGetValue(from,out var allowed)&&allowed.Contains(to);
    public static IReadOnlyList<string> Next(string from)=>Transitions.GetValueOrDefault(from)??[];
}

