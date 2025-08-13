public class MarketData
{
    public iEconAgent Company;
    public Good Good;
    public decimal Bid;
    public decimal Ask;

    public override string ToString()
    {
        return $"Company: {Company}, Good: {Good}, Bid: {Bid}, Ask: {Ask}";
    }
    public override bool Equals(object other)
    {
        if(other is MarketData data)
        {
            return Company.Equals(data.Company) && Good.Equals(data.Good) && Bid == data.Bid && Ask == data.Ask;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return Company.GetHashCode() + Good.GetHashCode() + Bid.GetHashCode() + Ask.GetHashCode();
    }
}