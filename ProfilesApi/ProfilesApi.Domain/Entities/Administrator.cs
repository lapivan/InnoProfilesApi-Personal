using ProfilesApi.Domain.Common;

namespace ProfilesApi.Domain.Entities;

public sealed class Administrator : SoftDeletableEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; }
    public Guid OfficeId { get; set; }
    public Office Office { get; set; }
       public DateTime CareerStartDate { get; init; }
    public int GapInMonths { get; set; }

    public int TotalExperience
    {
        get
        {
            // Some comment i really need
              var totalMonth = ((DateTime.UtcNow.Year - CareerStartDate.Year) * 12) + DateTime.UtcNow.Month - CareerStartDate.Month;
            if (DateTime.UtcNow.Day < CareerStartDate.Day)
            {
                --totalMonth;
            }
            var result = totalMonth - GapInMonths;
            return result;
        }
    }
    public  Administrator(Guid accountId, Guid officeId, DateTime careerStartDate, int gapInMonths)
    {
        AccountId = accountId;
        OfficeId = officeId;
        CareerStartDate = careerStartDate;
        GapInMonths = gapInMonths;
    }
    protected Administrator() { }
}