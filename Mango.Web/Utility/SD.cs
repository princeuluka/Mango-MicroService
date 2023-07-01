namespace Mango.Web.Utility
{
    public class SD
    {
        public static string CouponAPIBase { get; set; }
        public static string AuthAPIBase { get; set; }

        public const string RoleAdmin = "ADMIN";
        public const string RoleCustomer = "CUSTOMER";
        public const string Tokencookie = "JWTToken";

        public enum Apitype
        {
            GET,
            POST,
            PUT,
            DELETE,
            
        }
        
    }
}
