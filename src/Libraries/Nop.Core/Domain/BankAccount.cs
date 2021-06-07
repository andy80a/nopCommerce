namespace Nop.Core.Domain.Forums
{
    
    public partial class BankAccount : BaseEntity
    {
        public static int BANKACCOUNTANDRIYMACHUGA = 14;
        public int GroupId { get; set; }

        public string AccountName { get; set; }
        public string AccountNameUkr { get; set; }
        public string Account { get; set; }
        public string AccountDetails { get; set; }

        public int Probability { get; set; }
        public bool IsActive { get; set; }
  
    }
}
