namespace DAL.Constants
{
    public static class DataSchemaConstants
    {
        // String Lengths
        public const int DefaultNameLength = 256;
        public const int ShortNameLength = 100;
        public const int MaxUrlLength = 500;
        public const int MaxDescriptionLength = 2000;
        public const int TokenLength = 1000;
        public const int CurrencyLength = 3;
        public const int CouponCodeLength = 50;

        // Default Values
        public const string DefaultCurrency = "EGP";

        // SQL Column Types
        public const string MoneyColumnType = "decimal(18, 2)";
        public const string CoordinateColumnType = "decimal(10, 7)";
        public const string PercentageColumnType = "decimal(5, 2)";
    }
}