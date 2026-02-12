namespace Haley.Enums {
    public enum PlainAuthMode {
        Basic = 0,
        HeaderAuthToken, //Not necessarily JWT.. Its just a token, which is found in the bearer header of the Header. It can be JWT or any other token, and the validation is done by the user defined validator function.
        HeaderApiKey,
        Cookie, 
        AzureSAML,
        QueryToken,
        FormToken,
        HeaderJWT, //Specifically for JWT tokens in the header. The token is expected as a Bearer Token in the Authorization Header.
    }
}
