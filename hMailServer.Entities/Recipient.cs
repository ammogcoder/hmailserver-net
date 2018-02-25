namespace hMailServer.Entities
{
    public class Recipient
    {
        public long Id { get; set; }
        public long MessageId { get; set; }
        public string Address { get; set; }
        public string OriginalAddress { get; set; }
        public long AccountId { get; set; }

        public Recipient Clone()
        {
            return (Recipient) MemberwiseClone();
        }
    }
}
