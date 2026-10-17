using DAS.Models.DTOs;

namespace DAS.Models.ViewModels;

public class PhysicalCountResultViewModel
{
    public PhysicalCountResultViewModel(PhysicalCountDto count)
    {
        Count = count;
    }

    public PhysicalCountDto Count { get; }

    public IReadOnlyList<PhysicalCountLineDto> Lines => Count.Lines;
    public int MatchCount => Lines.Count(line => line.Difference == 0);
    public int ShortageCount => Lines.Count(line => line.Difference < 0);
    public int SurplusCount => Lines.Count(line => line.Difference > 0);
    public decimal ShortageCost => Lines.Where(line => line.Difference < 0).Sum(line => -line.DifferenceCost);
}
