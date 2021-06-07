namespace Nop.Core.Domain
{
    public partial class BankCard : BaseEntity
    {
        public static int BANKCARDGROUPIDANDRIYMACHUGA = 1;
        public int GroupId { get; set; }

        public string CardName { get; set; }
        public string CardNameUkr { get; set; }
        public string Card { get; set; }

        public int Probability { get; set; }
        public bool IsActive { get; set; }

        public string BankName { get; set; }
        public string BankNameUkr { get; set; }
    }
}
